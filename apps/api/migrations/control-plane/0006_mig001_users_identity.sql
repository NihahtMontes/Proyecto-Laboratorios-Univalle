-- MIG-001-F2-W2: additive identity contract (0006).
--
-- Purely additive, data-preserving on top of the immutable baseline
-- 0001..0005. No DROP TABLE, no DROP COLUMN, no TRUNCATE, no DELETE,
-- no hard delete, no password reset, no change to existing rows beyond the
-- deterministic disabled->inactive backfill and the strict pre-F2
-- password_hash classification: only exact bcrypt-shape hashes survive
-- byte-for-byte; everything else becomes reset_required with a NULL hash.
--
-- Requires PostgreSQL >= 13 (for built-in normalize(NFKC)) and a UTF8
-- server encoding. The migration is fail-closed if lu_auth_runtime is
-- missing, if the encoding is wrong, or if canonical invariants cannot be
-- enforced. Every new trigger function is SECURITY DEFINER with a fixed
-- pg_catalog/public search_path and is owned by the migration executor; the
-- EXECUTE privilege is granted only where the trigger path requires it.
--
-- The runtime role gets SELECT on every new surface; INSERT/UPDATE only on
-- the columns F3 actually writes; no DDL; no direct DML on claims, xref,
-- migration state, or migration history.

BEGIN;

SELECT pg_advisory_xact_lock(hashtext('lu:identity-control-plane:0006'));

-- ============================================================================
-- Step 0: server-side preflight (UTF8, PG >= 13, ICU root collation).
-- ============================================================================
DO $do$
DECLARE
  v_encoding text;
  v_version_int int;
BEGIN
  v_encoding := pg_catalog.current_setting('server_encoding');
  IF v_encoding NOT IN ('UTF8', 'UTF-8') THEN
    RAISE EXCEPTION 'lu_mig001 requires UTF8 server_encoding, got "%"', v_encoding;
  END IF;

  v_version_int := pg_catalog.current_setting('server_version_num')::int;
  IF v_version_int < 130000 THEN
    RAISE EXCEPTION 'lu_mig001 requires PostgreSQL >= 13 (built-in normalize(NFKC)); got "%"', v_version_int;
  END IF;

  IF NOT EXISTS (
    SELECT 1
      FROM pg_catalog.pg_collation c
      JOIN pg_catalog.pg_namespace n ON n.oid = c.collnamespace
     WHERE c.collname = 'und-x-icu'
       AND n.nspname = 'pg_catalog'
  ) THEN
    RAISE EXCEPTION 'lu_mig001 requires the pg_catalog."und-x-icu" root ICU collation';
  END IF;
END
$do$;

-- Fail-closed if lu_auth_runtime is absent.
DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime') THEN
    RAISE EXCEPTION 'lu_mig001 requires the lu_auth_runtime role to exist before applying 0006';
  END IF;
END
$do$;

-- ============================================================================
-- Step 1: account_status as F1 authority. Drop the legacy 2-state check on
-- status, backfill disabled->inactive deterministically, then install the
-- expanded F1 check.
-- ============================================================================

ALTER TABLE public.lu_user
  ADD COLUMN IF NOT EXISTS account_status text;

ALTER TABLE public.lu_user
  DROP CONSTRAINT IF EXISTS lu_user_status_check;

UPDATE public.lu_user
   SET account_status = CASE WHEN status = 'disabled' THEN 'inactive' ELSE 'active' END
 WHERE account_status IS NULL;

ALTER TABLE public.lu_user
  ALTER COLUMN account_status SET NOT NULL,
  ALTER COLUMN account_status SET DEFAULT 'active';

-- Synchronize the legacy shadow column to mirror account_status for existing
-- rows; the shadow is kept for pre-F3 compatibility only.
UPDATE public.lu_user
   SET status = account_status
 WHERE status IS DISTINCT FROM account_status
   AND status IN ('active', 'disabled');

-- ============================================================================
-- Step 2: reconciliation_state with deterministic backfill. Pre-F2 rows
-- stay pending; future inserts default to canonical (see step 7).
-- ============================================================================

ALTER TABLE public.lu_user
  ADD COLUMN IF NOT EXISTS reconciliation_state text;

UPDATE public.lu_user
   SET reconciliation_state = 'pending_reconciliation'
 WHERE reconciliation_state IS NULL;

-- ============================================================================
-- Step 3: canonical identity fields. Nullable during pending; required for
-- canonical via ck_lu_user_canonical_required_fields below.
-- ============================================================================

ALTER TABLE public.lu_user
  ADD COLUMN IF NOT EXISTS username                varchar(256),
  ADD COLUMN IF NOT EXISTS first_name              varchar(100),
  ADD COLUMN IF NOT EXISTS last_name               varchar(100),
  ADD COLUMN IF NOT EXISTS second_last_name        varchar(100),
  ADD COLUMN IF NOT EXISTS identity_card           varchar(10),
  ADD COLUMN IF NOT EXISTS phone_number            varchar(30),
  ADD COLUMN IF NOT EXISTS profile_picture_key     varchar(512),
  ADD COLUMN IF NOT EXISTS password_scheme         text,
  ADD COLUMN IF NOT EXISTS must_change_password    boolean,
  ADD COLUMN IF NOT EXISTS password_migrated_at    timestamptz,
  ADD COLUMN IF NOT EXISTS row_version             bigint,
  ADD COLUMN IF NOT EXISTS created_by_user_id      uuid,
  ADD COLUMN IF NOT EXISTS modified_by_user_id     uuid;

ALTER TABLE public.lu_user
  ALTER COLUMN password_scheme      SET DEFAULT 'bcrypt',
  ALTER COLUMN must_change_password SET DEFAULT true,
  ALTER COLUMN row_version          SET DEFAULT 0;

-- Replace the immutable-baseline nonblank-hash check before making the column
-- nullable. reset_required is the only scheme allowed to carry a NULL hash.
ALTER TABLE public.lu_user
  DROP CONSTRAINT IF EXISTS lu_user_password_hash_check;

ALTER TABLE public.lu_user
  ALTER COLUMN password_hash DROP NOT NULL;

-- Deterministic pre-F2 classification (MIG-001-F2-W14B): an existing non-null
-- hash is classified bcrypt only when it matches the canonical bcrypt shape
-- exactly ($2a/$2b/$2y, cost 04..31, 53 payload chars); that hash is preserved
-- byte-for-byte and the account is NOT forced to change. Null/blank/malformed
-- or invalid-cost hashes are classified reset_required with a NULL hash and
-- must_change_password=true. SQL Server Identity V2/V3 hashes are NOT
-- classified here: those rows are future mapper loads that carry an explicit
-- scheme. No hash value is ever logged or exposed.
UPDATE public.lu_user
   SET password_scheme = CASE
         WHEN password_hash ~ '^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$'
           THEN 'bcrypt'
         ELSE 'reset_required'
       END,
       must_change_password = CASE
         WHEN password_hash ~ '^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$'
           THEN false
         ELSE true
       END,
       password_hash = CASE
         WHEN password_hash ~ '^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$'
           THEN password_hash
         ELSE NULL
       END,
       row_version = COALESCE(row_version, 0)
 WHERE password_scheme IS NULL
    OR must_change_password IS NULL
    OR row_version IS NULL;

-- ============================================================================
-- Step 4: install the F1 password state CHECKs and tighten NOT NULL.
-- ============================================================================

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_password_scheme') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_password_scheme
      CHECK (password_scheme IN
             ('legacy_identity_v2', 'legacy_identity_v3', 'bcrypt', 'reset_required'));
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_password_hash_conditional') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_password_hash_conditional
      CHECK (
        (password_scheme = 'bcrypt'
          AND password_hash ~ '^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$')
        OR (password_scheme = 'reset_required' AND password_hash IS NULL)
        OR (password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3')
            AND password_hash IS NOT NULL
            AND length(btrim(password_hash)) > 0)
      );
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_must_change_password') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_must_change_password
      CHECK (
        (password_scheme = 'reset_required' AND must_change_password = true)
        OR (password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3')
            AND must_change_password = false)
        OR (password_scheme = 'bcrypt'
            AND must_change_password IN (true, false))
      );
  END IF;
END
$do$;

ALTER TABLE public.lu_user
  ALTER COLUMN password_scheme      SET NOT NULL,
  ALTER COLUMN must_change_password SET NOT NULL,
  ALTER COLUMN row_version          SET NOT NULL,
  ALTER COLUMN reconciliation_state SET NOT NULL;

-- ============================================================================
-- Step 5: length + format + canonical-required CHECKs.
-- ============================================================================

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_username_length') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_username_length
      CHECK (username IS NULL OR length(username) <= 256);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_email_length') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_email_length
      CHECK (length(email) <= 256);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_first_name_length') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_first_name_length
      CHECK (first_name IS NULL OR length(first_name) <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_last_name_length') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_last_name_length
      CHECK (last_name IS NULL OR length(last_name) <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_second_last_name_length') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_second_last_name_length
      CHECK (second_last_name IS NULL OR length(second_last_name) <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_identity_card_format') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_identity_card_format
      CHECK (identity_card IS NULL OR identity_card ~ '^[0-9A-Z-]{1,10}$');
  END IF;
  -- F1/W14A phone syntax: nullable while pending; non-null values are at most
  -- 30 chars, allow one optional leading +, digits/spaces/dot/hyphen/
  -- parentheses only, and carry 7..15 total digits. The regex is
  -- normalization-free; the digit count is a regexp_replace expression.
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_phone_number_format') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_phone_number_format
      CHECK (
        phone_number IS NULL
        OR (
          length(phone_number) <= 30
          AND phone_number ~ '^[+]?[0-9 ().-]+$'
          AND length(regexp_replace(phone_number, '[^0-9]', '', 'g')) >= 7
          AND length(regexp_replace(phone_number, '[^0-9]', '', 'g')) <= 15
        )
      );
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_profile_picture_key_length') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_profile_picture_key_length
      CHECK (profile_picture_key IS NULL OR length(profile_picture_key) <= 512);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_row_version') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_row_version
      CHECK (row_version >= 0);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_reconciliation_state') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_reconciliation_state
      CHECK (reconciliation_state IN ('pending_reconciliation', 'canonical'));
  END IF;
  -- Canonical rows must have every split-name/identity field set.
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_canonical_required_fields') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_canonical_required_fields
      CHECK (
        (reconciliation_state = 'pending_reconciliation')
        OR
        (reconciliation_state = 'canonical'
          AND username IS NOT NULL AND length(btrim(username)) > 0
          AND email IS NOT NULL AND length(btrim(email)) > 0
          AND first_name IS NOT NULL AND length(btrim(first_name)) > 0
          AND last_name IS NOT NULL AND length(btrim(last_name)) > 0
          AND identity_card IS NOT NULL AND length(btrim(identity_card)) > 0
          AND phone_number IS NOT NULL AND length(btrim(phone_number)) > 0)
      );
  END IF;
END
$do$;

-- ============================================================================
-- Step 6: actor FKs (RESTRICT).
-- ============================================================================

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'fk_lu_user_created_by') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT fk_lu_user_created_by
      FOREIGN KEY (created_by_user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'fk_lu_user_modified_by') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT fk_lu_user_modified_by
      FOREIGN KEY (modified_by_user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT;
  END IF;
END
$do$;

-- ============================================================================
-- Step 7: status CHECKs and non-divergence BEFORE trigger.
-- account_status is the F1 authority. status remains as a transitional shadow.
-- Legacy disabled is rewritten to inactive before the three-state CHECK runs.
-- ============================================================================

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_account_status') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_account_status
      CHECK (account_status IN ('active', 'inactive', 'deleted'));
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_user_status_shadow') THEN
    ALTER TABLE public.lu_user
      ADD CONSTRAINT ck_lu_user_status_shadow
      CHECK (status IN ('active', 'inactive', 'deleted'));
  END IF;
END
$do$;

-- Future default for new inserts is 'canonical' (not pending) so callers
-- never accidentally create pending rows by default. Existing rows were
-- backfilled to 'pending_reconciliation' in step 2.
ALTER TABLE public.lu_user
  ALTER COLUMN reconciliation_state SET DEFAULT 'canonical';

CREATE OR REPLACE FUNCTION public.lu_user_status_no_diverge()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
DECLARE
  v_status_mapped text;
  v_status_changed boolean;
  v_account_changed boolean;
BEGIN
  v_status_mapped := CASE WHEN NEW.status = 'disabled' THEN 'inactive' ELSE NEW.status END;

  IF TG_OP = 'INSERT' THEN
    -- Defaults make an omitted side appear as active. Resolve that ambiguity in
    -- favor of the non-default side; otherwise both explicit values must agree.
    IF v_status_mapped = NEW.account_status THEN
      NEW.status := NEW.account_status;
    ELSIF NEW.account_status = 'active' AND v_status_mapped <> 'active' THEN
      NEW.account_status := v_status_mapped;
      NEW.status := v_status_mapped;
    ELSIF v_status_mapped = 'active' AND NEW.account_status <> 'active' THEN
      NEW.status := NEW.account_status;
    ELSE
      RAISE EXCEPTION 'lu_user status/account_status values conflict on insert';
    END IF;
    RETURN NEW;
  END IF;

  v_status_changed := NEW.status IS DISTINCT FROM OLD.status;
  v_account_changed := NEW.account_status IS DISTINCT FROM OLD.account_status;

  IF v_status_changed AND v_account_changed THEN
    IF v_status_mapped IS DISTINCT FROM NEW.account_status THEN
      RAISE EXCEPTION 'lu_user status/account_status values conflict on update';
    END IF;
    NEW.status := NEW.account_status;
  ELSIF v_status_changed THEN
    NEW.account_status := v_status_mapped;
    NEW.status := v_status_mapped;
  ELSIF v_account_changed THEN
    NEW.status := NEW.account_status;
  ELSE
    NEW.status := NEW.account_status;
  END IF;

  RETURN NEW;
END
$func$;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname = 'trg_lu_user_status_no_diverge') THEN
    CREATE TRIGGER trg_lu_user_status_no_diverge
      BEFORE INSERT OR UPDATE OF status, account_status
      ON public.lu_user
      FOR EACH ROW
      EXECUTE FUNCTION public.lu_user_status_no_diverge();
  END IF;
END
$do$;

REVOKE EXECUTE ON FUNCTION public.lu_user_status_no_diverge() FROM PUBLIC;

-- ============================================================================
-- Step 8: indexes. 0001 already provides ux_lu_user_email_lower; we add the
-- canonical username CI and identity-card CI unique indexes plus audit
-- indexes. No duplicate lower(email) index.
-- ============================================================================

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_user_username_ci
  ON public.lu_user (lower(username))
  WHERE username IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_user_identity_card_ci
  ON public.lu_user (upper(identity_card))
  WHERE identity_card IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_lu_user_account_status
  ON public.lu_user (account_status);

CREATE INDEX IF NOT EXISTS ix_lu_user_security_version
  ON public.lu_user (security_version);

CREATE INDEX IF NOT EXISTS ix_lu_user_reconciliation_state
  ON public.lu_user (reconciliation_state);

CREATE INDEX IF NOT EXISTS ix_lu_user_modified_by
  ON public.lu_user (modified_by_user_id)
  WHERE modified_by_user_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_lu_user_created_by
  ON public.lu_user (created_by_user_id)
  WHERE created_by_user_id IS NOT NULL;

-- ============================================================================
-- Step 9: full_name derivation BEFORE trigger. Pending rows keep their
-- original full_name byte-for-byte; canonical rows get one-space-joined
-- split names; canonical rows must have non-blank first + last.
-- ============================================================================

CREATE OR REPLACE FUNCTION public.lu_user_full_name_sync()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
DECLARE
  v_first text;
  v_last text;
  v_second text;
  v_computed text;
BEGIN
  IF TG_OP = 'UPDATE'
     AND OLD.reconciliation_state = 'canonical'
     AND NEW.reconciliation_state = 'pending_reconciliation' THEN
    RAISE EXCEPTION 'lu_user canonical to pending_reconciliation transition is forbidden';
  END IF;

  IF NEW.reconciliation_state = 'canonical' THEN
    NEW.username := public.lu_login_identifier_normalize(NEW.username);
    NEW.email := public.lu_login_identifier_normalize(NEW.email);
    NEW.identity_card := public.lu_user_identity_card_normalize(NEW.identity_card);
    NEW.phone_number := public.lu_user_phone_normalize(NEW.phone_number);
    NEW.first_name := public.lu_user_split_name_normalize(NEW.first_name);
    NEW.last_name := public.lu_user_split_name_normalize(NEW.last_name);
    IF NEW.second_last_name IS NOT NULL THEN
      NEW.second_last_name := NULLIF(
        public.lu_user_split_name_normalize(NEW.second_last_name),
        ''
      );
    END IF;

    IF TG_OP = 'UPDATE'
       AND OLD.reconciliation_state = 'canonical'
       AND NEW.username IS DISTINCT FROM OLD.username THEN
      RAISE EXCEPTION 'lu_user.username is immutable after canonical reconciliation';
    END IF;

    v_first := NEW.first_name;
    v_last := NEW.last_name;
    IF v_first IS NULL OR v_first = '' OR v_last IS NULL OR v_last = '' THEN
      RAISE EXCEPTION
        'lu_user canonical row requires non-empty first_name and last_name (user id=%)', NEW.id;
    END IF;
    v_computed := v_first || ' ' || v_last;
    IF NEW.second_last_name IS NOT NULL AND btrim(NEW.second_last_name) <> '' THEN
      v_computed := v_computed || ' ' || btrim(NEW.second_last_name);
    END IF;
    NEW.full_name := v_computed;
  END IF;
  RETURN NEW;
END
$func$;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname = 'trg_lu_user_full_name_sync') THEN
    CREATE TRIGGER trg_lu_user_full_name_sync
      BEFORE INSERT OR UPDATE
      ON public.lu_user
      FOR EACH ROW
      EXECUTE FUNCTION public.lu_user_full_name_sync();
  END IF;
END
$do$;

REVOKE EXECUTE ON FUNCTION public.lu_user_full_name_sync() FROM PUBLIC;

-- ============================================================================
-- Step 10: row_version + updated_at triggers for lu_user + immutable
-- created_at/created_by trigger. row_version is incremented by the trigger
-- only; client-supplied updates to it are rejected.
-- ============================================================================

CREATE OR REPLACE FUNCTION public.lu_user_row_version_inc()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.row_version IS NULL THEN
      NEW.row_version := 0;
    ELSIF NEW.row_version <> 0 THEN
      RAISE EXCEPTION 'lu_user.row_version must start at zero';
    END IF;
    NEW.created_at := COALESCE(NEW.created_at, CURRENT_TIMESTAMP);
    NEW.updated_at := COALESCE(NEW.updated_at, CURRENT_TIMESTAMP);
  ELSIF TG_OP = 'UPDATE' THEN
    IF NEW.row_version IS DISTINCT FROM OLD.row_version THEN
      RAISE EXCEPTION 'lu_user.row_version is server-managed';
    END IF;
    NEW.row_version := OLD.row_version + 1;
    NEW.updated_at := CURRENT_TIMESTAMP;
  END IF;
  RETURN NEW;
END
$func$;

CREATE OR REPLACE FUNCTION public.lu_user_immutable_created()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  IF TG_OP = 'UPDATE' THEN
    IF NEW.created_at IS DISTINCT FROM OLD.created_at THEN
      RAISE EXCEPTION 'lu_user.created_at is immutable';
    END IF;
    IF NEW.created_by_user_id IS DISTINCT FROM OLD.created_by_user_id THEN
      RAISE EXCEPTION 'lu_user.created_by_user_id is immutable';
    END IF;
  END IF;
  RETURN NEW;
END
$func$;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname = 'trg_lu_user_row_version_inc') THEN
    CREATE TRIGGER trg_lu_user_row_version_inc
      BEFORE INSERT OR UPDATE ON public.lu_user
      FOR EACH ROW
      EXECUTE FUNCTION public.lu_user_row_version_inc();
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname = 'trg_lu_user_immutable_created') THEN
    CREATE TRIGGER trg_lu_user_immutable_created
      BEFORE UPDATE ON public.lu_user
      FOR EACH ROW
      EXECUTE FUNCTION public.lu_user_immutable_created();
  END IF;
END
$do$;

REVOKE EXECUTE ON FUNCTION public.lu_user_row_version_inc() FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION public.lu_user_immutable_created() FROM PUBLIC;

-- ============================================================================
-- Step 11: NFKC-aware single normalizer + claim projection table.
-- ============================================================================

-- Built-in normalize(text, NFKC) is the canonical NFKC normalizer; we wrap
-- it for parity with the migration's identity-card (uppercase) and split
-- name (NFC, preserve case) helpers.

CREATE OR REPLACE FUNCTION public.lu_login_identifier_normalize(value text)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
  SELECT lower(pg_catalog.normalize(btrim(value), 'NFKC') COLLATE "und-x-icu");
$func$;

CREATE OR REPLACE FUNCTION public.lu_user_identity_card_normalize(value text)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
  SELECT upper(pg_catalog.normalize(btrim(value), 'NFKC') COLLATE "und-x-icu");
$func$;

CREATE OR REPLACE FUNCTION public.lu_user_split_name_normalize(value text)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
  -- Names use NFC per F1: preserve case and collapse internal whitespace.
  SELECT pg_catalog.regexp_replace(
    pg_catalog.normalize(btrim(value), 'NFC'),
    '[[:space:]]+',
    ' ',
    'g'
  );
$func$;

CREATE OR REPLACE FUNCTION public.lu_user_phone_normalize(value text)
RETURNS text
LANGUAGE sql
IMMUTABLE
PARALLEL SAFE
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
  SELECT btrim(value);
$func$;

-- The claim projection table is owned by the migration executor; runtime
-- gets SELECT only, EXECUTE on the trigger function is granted only where
-- needed. ALL revoked from PUBLIC for trigger functions (defense in depth).

CREATE TABLE IF NOT EXISTS public.lu_login_identifier (
  id                uuid        PRIMARY KEY,
  user_id           uuid        NOT NULL,
  kind              text        NOT NULL,
  normalized_value  varchar(256) NOT NULL,
  created_at        timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT ck_lu_login_identifier_kind
    CHECK (kind IN ('username', 'email')),
  CONSTRAINT ck_lu_login_identifier_value_nonempty
    CHECK (length(btrim(normalized_value)) > 0),
  CONSTRAINT ck_lu_login_identifier_value_length
    CHECK (length(normalized_value) <= 256),
  CONSTRAINT ck_lu_login_identifier_value_normalized
    CHECK (normalized_value = public.lu_login_identifier_normalize(normalized_value)),
  CONSTRAINT fk_lu_login_identifier_user
    FOREIGN KEY (user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_login_identifier_normalized_value
  ON public.lu_login_identifier (normalized_value);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_login_identifier_user_kind
  ON public.lu_login_identifier (user_id, kind);

-- ============================================================================
-- Step 12: claim sync trigger (SECURITY DEFINER, fixed search_path, owned
-- by migration executor). Backfills the email claim; canonical rows also
-- get a username claim. canonical -> pending is rejected so we never need
-- to DELETE claims inside the migration.
-- ============================================================================

CREATE OR REPLACE FUNCTION public.lu_user_sync_claims()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
DECLARE
  v_email text;
  v_username text;
BEGIN
  -- Lock the parent row to serialize against the deferred invariant trigger.
  PERFORM 1 FROM public.lu_user WHERE id = NEW.id FOR KEY SHARE;

  IF TG_OP = 'UPDATE' AND OLD.reconciliation_state = 'canonical'
     AND NEW.reconciliation_state = 'pending_reconciliation' THEN
    RAISE EXCEPTION
      'lu_user canonical -> pending transition is forbidden (would orphan a username claim)';
  END IF;

  v_email := CASE
    WHEN NEW.email IS NOT NULL THEN public.lu_login_identifier_normalize(NEW.email)
    ELSE NULL
  END;

  IF v_email IS NOT NULL THEN
    INSERT INTO public.lu_login_identifier (id, user_id, kind, normalized_value)
    VALUES (pg_catalog.gen_random_uuid(), NEW.id, 'email', v_email)
    ON CONFLICT (user_id, kind) DO UPDATE SET normalized_value = EXCLUDED.normalized_value;
  END IF;

  IF NEW.reconciliation_state = 'canonical' THEN
    v_username := public.lu_login_identifier_normalize(NEW.username);
    INSERT INTO public.lu_login_identifier (id, user_id, kind, normalized_value)
    VALUES (pg_catalog.gen_random_uuid(), NEW.id, 'username', v_username)
    ON CONFLICT (user_id, kind) DO UPDATE SET normalized_value = EXCLUDED.normalized_value;
  END IF;

  RETURN NEW;
END
$func$;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname = 'trg_lu_user_sync_claims') THEN
    CREATE TRIGGER trg_lu_user_sync_claims
      AFTER INSERT OR UPDATE OF email, username, reconciliation_state
      ON public.lu_user
      FOR EACH ROW
      EXECUTE FUNCTION public.lu_user_sync_claims();
  END IF;
END
$do$;

-- Revoke EXECUTE on claim trigger functions from PUBLIC.
REVOKE EXECUTE ON FUNCTION public.lu_user_sync_claims() FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION public.lu_login_identifier_normalize(text) FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION public.lu_user_identity_card_normalize(text) FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION public.lu_user_split_name_normalize(text) FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION public.lu_user_phone_normalize(text) FROM PUBLIC;

-- ============================================================================
-- Step 13: two-claim deferred invariant trigger. Inspects the affected
-- user's reconciliation_state. Locks the parent row. Fires on BOTH
-- lu_login_identifier changes (DEFERRABLE INITIALLY DEFERRED) and
-- lu_user changes (DEFERRABLE INITIALLY DEFERRED) so a single transaction
-- can stage both sides without partial-state failures.
-- ============================================================================

CREATE OR REPLACE FUNCTION public.lu_login_identifier_two_claims_check()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
DECLARE
  v_user_id uuid;
  v_state text;
  v_expected_email text;
  v_expected_username text;
  v_email_count bigint;
  v_username_count bigint;
  v_matching_email_count bigint;
  v_matching_username_count bigint;
BEGIN
  IF TG_TABLE_NAME = 'lu_user' THEN
    v_user_id := NEW.id;
  ELSIF TG_OP = 'DELETE' THEN
    v_user_id := OLD.user_id;
  ELSE
    v_user_id := NEW.user_id;
  END IF;

  SELECT u.reconciliation_state,
         public.lu_login_identifier_normalize(u.email),
         CASE
           WHEN u.reconciliation_state = 'canonical'
             THEN public.lu_login_identifier_normalize(u.username)
           ELSE NULL
         END
    INTO v_state, v_expected_email, v_expected_username
    FROM public.lu_user u
   WHERE u.id = v_user_id
   FOR KEY SHARE;

  IF NOT FOUND THEN
    RAISE EXCEPTION 'lu_login_identifier references missing lu_user (%)', v_user_id;
  END IF;

  SELECT count(*) FILTER (WHERE li.kind = 'email'),
         count(*) FILTER (WHERE li.kind = 'username'),
         count(*) FILTER (
           WHERE li.kind = 'email'
             AND li.normalized_value = v_expected_email
         ),
         count(*) FILTER (
           WHERE li.kind = 'username'
             AND li.normalized_value = v_expected_username
         )
    INTO v_email_count,
         v_username_count,
         v_matching_email_count,
         v_matching_username_count
    FROM public.lu_login_identifier li
   WHERE li.user_id = v_user_id;

  IF v_state = 'pending_reconciliation' THEN
    IF v_email_count <> 1 OR v_matching_email_count <> 1 OR v_username_count <> 0 THEN
      RAISE EXCEPTION 'pending lu_user claim projection is inconsistent (user id=%)', v_user_id;
    END IF;
  ELSIF v_state = 'canonical' THEN
    IF v_email_count <> 1 OR v_username_count <> 1
       OR v_matching_email_count <> 1 OR v_matching_username_count <> 1 THEN
      RAISE EXCEPTION 'canonical lu_user claim projection is inconsistent (user id=%)', v_user_id;
    END IF;
  END IF;

  IF TG_OP = 'DELETE' THEN RETURN OLD; ELSE RETURN NEW; END IF;
END
$func$;

CREATE CONSTRAINT TRIGGER trg_lu_login_identifier_two_claims
  AFTER INSERT OR UPDATE OR DELETE ON public.lu_login_identifier
  DEFERRABLE INITIALLY DEFERRED
  FOR EACH ROW
  EXECUTE FUNCTION public.lu_login_identifier_two_claims_check();

CREATE CONSTRAINT TRIGGER trg_lu_user_two_claims_invariant
  AFTER INSERT OR UPDATE OF reconciliation_state, email, username ON public.lu_user
  DEFERRABLE INITIALLY DEFERRED
  FOR EACH ROW
  EXECUTE FUNCTION public.lu_login_identifier_two_claims_check();

REVOKE EXECUTE ON FUNCTION public.lu_login_identifier_two_claims_check() FROM PUBLIC;

-- Existing target users were marked pending_reconciliation above. They receive
-- only their real normalized email claim; no username is inferred.
INSERT INTO public.lu_login_identifier (id, user_id, kind, normalized_value)
SELECT pg_catalog.gen_random_uuid(),
       u.id,
       'email',
       public.lu_login_identifier_normalize(u.email)
  FROM public.lu_user u
 WHERE u.email IS NOT NULL
ON CONFLICT (user_id, kind) DO NOTHING;

-- Defensive support for a pre-existing canonical row in an idempotent upgrade.
INSERT INTO public.lu_login_identifier (id, user_id, kind, normalized_value)
SELECT pg_catalog.gen_random_uuid(),
       u.id,
       'username',
       public.lu_login_identifier_normalize(u.username)
  FROM public.lu_user u
 WHERE u.reconciliation_state = 'canonical'
   AND u.username IS NOT NULL
ON CONFLICT (user_id, kind) DO NOTHING;

-- ============================================================================
-- Step 14: lu_legacy_user_xref.
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.lu_legacy_user_xref (
  id                 uuid        PRIMARY KEY,
  source_system      text        NOT NULL,
  legacy_user_id     integer     NOT NULL,
  user_id            uuid        NOT NULL,
  migration_run_id   text        NOT NULL,
  source_fingerprint text        NOT NULL,
  migrated_at        timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT ck_lu_legacy_user_xref_source_system
    CHECK (length(btrim(source_system)) > 0 AND length(source_system) <= 64),
  CONSTRAINT ck_lu_legacy_user_xref_legacy_user_id
    CHECK (legacy_user_id >= 0),
  CONSTRAINT ck_lu_legacy_user_xref_run_id
    CHECK (length(btrim(migration_run_id)) > 0),
  CONSTRAINT ck_lu_legacy_user_xref_fingerprint
    CHECK (length(btrim(source_fingerprint)) > 0),
  CONSTRAINT fk_lu_legacy_user_xref_user
    FOREIGN KEY (user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_legacy_user_xref_source_legacy
  ON public.lu_legacy_user_xref (source_system, legacy_user_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_legacy_user_xref_source_user
  ON public.lu_legacy_user_xref (source_system, user_id);

CREATE INDEX IF NOT EXISTS ix_lu_legacy_user_xref_user
  ON public.lu_legacy_user_xref (user_id);

CREATE OR REPLACE FUNCTION public.lu_legacy_user_xref_immutable()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  RAISE EXCEPTION 'lu_legacy_user_xref rows are permanent; UPDATE/DELETE forbidden';
END
$func$;

CREATE TRIGGER trg_lu_legacy_user_xref_no_update
  BEFORE UPDATE ON public.lu_legacy_user_xref
  FOR EACH ROW EXECUTE FUNCTION public.lu_legacy_user_xref_immutable();

CREATE TRIGGER trg_lu_legacy_user_xref_no_delete
  BEFORE DELETE ON public.lu_legacy_user_xref
  FOR EACH ROW EXECUTE FUNCTION public.lu_legacy_user_xref_immutable();

REVOKE EXECUTE ON FUNCTION public.lu_legacy_user_xref_immutable() FROM PUBLIC;

-- ============================================================================
-- Step 15: lu_identity_audit_event (append-only).
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.lu_identity_audit_event (
  id                 uuid        PRIMARY KEY,
  occurred_at        timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
  action             text        NOT NULL,
  subject_user_id    uuid        NULL,
  actor_user_id      uuid        NULL,
  site_id            uuid        NULL,
  correlation_id     uuid        NULL,
  reason             text        NULL,
  before_status      text        NULL,
  after_status       text        NULL,
  metadata           jsonb       NOT NULL DEFAULT '{}'::jsonb,
  CONSTRAINT ck_lu_identity_audit_event_action
    CHECK (length(btrim(action)) > 0 AND length(action) <= 128),
  CONSTRAINT ck_lu_identity_audit_event_before_status
    CHECK (before_status IS NULL OR before_status IN
           ('active', 'inactive', 'deleted')),
  CONSTRAINT ck_lu_identity_audit_event_after_status
    CHECK (after_status IS NULL OR after_status IN
           ('active', 'inactive', 'deleted')),
  CONSTRAINT ck_lu_identity_audit_event_metadata_object
    CHECK (pg_catalog.jsonb_typeof(metadata) = 'object'),
  CONSTRAINT fk_lu_identity_audit_event_subject
    FOREIGN KEY (subject_user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT,
  CONSTRAINT fk_lu_identity_audit_event_actor
    FOREIGN KEY (actor_user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT,
  CONSTRAINT fk_lu_identity_audit_event_site
    FOREIGN KEY (site_id) REFERENCES public.lu_site (id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_lu_identity_audit_event_subject_time
  ON public.lu_identity_audit_event (subject_user_id, occurred_at)
  WHERE subject_user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_lu_identity_audit_event_actor_time
  ON public.lu_identity_audit_event (actor_user_id, occurred_at)
  WHERE actor_user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_lu_identity_audit_event_site_time
  ON public.lu_identity_audit_event (site_id, occurred_at)
  WHERE site_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_lu_identity_audit_event_action_time
  ON public.lu_identity_audit_event (action, occurred_at);

CREATE OR REPLACE FUNCTION public.lu_identity_audit_event_append_only()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  RAISE EXCEPTION 'lu_identity_audit_event is append-only; UPDATE/DELETE forbidden by F1 audit contract';
END
$func$;

CREATE TRIGGER trg_lu_identity_audit_event_no_update
  BEFORE UPDATE ON public.lu_identity_audit_event
  FOR EACH ROW EXECUTE FUNCTION public.lu_identity_audit_event_append_only();

CREATE TRIGGER trg_lu_identity_audit_event_no_delete
  BEFORE DELETE ON public.lu_identity_audit_event
  FOR EACH ROW EXECUTE FUNCTION public.lu_identity_audit_event_append_only();

REVOKE EXECUTE ON FUNCTION public.lu_identity_audit_event_append_only() FROM PUBLIC;

-- ============================================================================
-- Step 16: lu_identity_migration_state (cutover). No INSERT/UPDATE here:
-- F2 never sets cutover timestamps. Both columns stay NULL.
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.lu_identity_migration_state (
  id                          smallint PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  writer_label                text NULL,
  cutover_at                  timestamptz NULL,
  legacy_password_deadline    timestamptz NULL,
  created_at                  timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at                  timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT ck_lu_identity_migration_state_writer_label
    CHECK (writer_label IS NULL OR writer_label IN
           ('LEGACY_SQLSERVER', 'NEST_SQLSERVER', 'NEST_POSTGRES')),
  CONSTRAINT ck_lu_identity_migration_state_deadline
    CHECK (
      (cutover_at IS NULL AND legacy_password_deadline IS NULL)
      OR (cutover_at IS NOT NULL
          AND legacy_password_deadline IS NOT NULL
          AND legacy_password_deadline = cutover_at + INTERVAL '90 days')
    ),
  CONSTRAINT ck_lu_identity_migration_state_updated
    CHECK (updated_at >= created_at)
);

CREATE OR REPLACE FUNCTION public.lu_identity_migration_state_lock()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  IF OLD.cutover_at IS NOT NULL AND NEW.cutover_at IS DISTINCT FROM OLD.cutover_at THEN
    RAISE EXCEPTION 'lu_identity_migration_state.cutover_at is immutable once set';
  END IF;
  IF OLD.legacy_password_deadline IS NOT NULL
     AND NEW.legacy_password_deadline IS DISTINCT FROM OLD.legacy_password_deadline THEN
    RAISE EXCEPTION 'lu_identity_migration_state.legacy_password_deadline is immutable once set';
  END IF;
  IF NEW.cutover_at IS NOT NULL AND NEW.legacy_password_deadline IS NULL THEN
    RAISE EXCEPTION 'legacy_password_deadline must be set together with cutover_at';
  END IF;
  NEW.updated_at := CURRENT_TIMESTAMP;
  RETURN NEW;
END
$func$;

CREATE TRIGGER trg_lu_identity_migration_state_lock
  BEFORE UPDATE ON public.lu_identity_migration_state
  FOR EACH ROW EXECUTE FUNCTION public.lu_identity_migration_state_lock();

REVOKE EXECUTE ON FUNCTION public.lu_identity_migration_state_lock() FROM PUBLIC;

-- ============================================================================
-- Step 17: lu_migration_history (control-plane stream). Pin rows are inserted
-- by the runner before this same transaction commits; ordinal >= 1; immutable;
-- UNIQUE(stream, ordinal)
-- + UNIQUE(stream, relative_path).
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.lu_migration_history (
  stream        text    NOT NULL,
  ordinal       integer NOT NULL,
  relative_path text    NOT NULL,
  sha256        char(64) NOT NULL,
  bytes         integer NOT NULL,
  work_id       text    NULL,
  applied_at    timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (stream, ordinal),
  UNIQUE (stream, relative_path),
  CONSTRAINT ck_lu_migration_history_stream
    CHECK (stream IN ('control-plane', 'tenant')),
  CONSTRAINT ck_lu_migration_history_ordinal
    CHECK (ordinal >= 1),
  CONSTRAINT ck_lu_migration_history_sha256
    CHECK (sha256 ~ '^[0-9a-f]{64}$'),
  CONSTRAINT ck_lu_migration_history_bytes
    CHECK (bytes > 0),
  CONSTRAINT ck_lu_migration_history_relative_path
    CHECK (length(relative_path) > 0)
);

CREATE INDEX IF NOT EXISTS ix_lu_migration_history_applied_at
  ON public.lu_migration_history (applied_at);

CREATE OR REPLACE FUNCTION public.lu_migration_history_immutable()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  RAISE EXCEPTION 'lu_migration_history rows are immutable (use a new ordinal); UPDATE/DELETE forbidden';
END
$func$;

CREATE TRIGGER trg_lu_migration_history_no_update
  BEFORE UPDATE ON public.lu_migration_history
  FOR EACH ROW EXECUTE FUNCTION public.lu_migration_history_immutable();

CREATE TRIGGER trg_lu_migration_history_no_delete
  BEFORE DELETE ON public.lu_migration_history
  FOR EACH ROW EXECUTE FUNCTION public.lu_migration_history_immutable();

REVOKE EXECUTE ON FUNCTION public.lu_migration_history_immutable() FROM PUBLIC;

-- ============================================================================
-- Step 18: lu_site_membership extensions.
-- ============================================================================

ALTER TABLE public.lu_site_membership
  ADD COLUMN IF NOT EXISTS position              varchar(100),
  ADD COLUMN IF NOT EXISTS department            varchar(100),
  ADD COLUMN IF NOT EXISTS hire_date             date,
  ADD COLUMN IF NOT EXISTS row_version           bigint,
  ADD COLUMN IF NOT EXISTS created_by_user_id    uuid,
  ADD COLUMN IF NOT EXISTS modified_by_user_id   uuid;

ALTER TABLE public.lu_site_membership
  ALTER COLUMN row_version SET DEFAULT 0;

UPDATE public.lu_site_membership
   SET row_version = COALESCE(row_version, 0)
 WHERE row_version IS NULL;

ALTER TABLE public.lu_site_membership
  ALTER COLUMN row_version SET NOT NULL;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_site_membership_row_version') THEN
    ALTER TABLE public.lu_site_membership
      ADD CONSTRAINT ck_lu_site_membership_row_version
      CHECK (row_version >= 0);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_site_membership_position') THEN
    ALTER TABLE public.lu_site_membership
      ADD CONSTRAINT ck_lu_site_membership_position
      CHECK (position IS NULL OR length(position) <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_site_membership_department') THEN
    ALTER TABLE public.lu_site_membership
      ADD CONSTRAINT ck_lu_site_membership_department
      CHECK (department IS NULL OR length(department) <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'fk_lu_site_membership_created_by') THEN
    ALTER TABLE public.lu_site_membership
      ADD CONSTRAINT fk_lu_site_membership_created_by
      FOREIGN KEY (created_by_user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'fk_lu_site_membership_modified_by') THEN
    ALTER TABLE public.lu_site_membership
      ADD CONSTRAINT fk_lu_site_membership_modified_by
      FOREIGN KEY (modified_by_user_id) REFERENCES public.lu_user (id) ON DELETE RESTRICT;
  END IF;
END
$do$;

CREATE INDEX IF NOT EXISTS ix_lu_site_membership_user_status
  ON public.lu_site_membership (user_id, status);

-- lu_site_membership row_version + updated_at + immutable created_at.
CREATE OR REPLACE FUNCTION public.lu_site_membership_row_version_inc()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.row_version IS NULL THEN
      NEW.row_version := 0;
    ELSIF NEW.row_version <> 0 THEN
      RAISE EXCEPTION 'lu_site_membership.row_version must start at zero';
    END IF;
    NEW.created_at := COALESCE(NEW.created_at, CURRENT_TIMESTAMP);
    NEW.updated_at := COALESCE(NEW.updated_at, CURRENT_TIMESTAMP);
  ELSIF TG_OP = 'UPDATE' THEN
    IF NEW.row_version IS DISTINCT FROM OLD.row_version THEN
      RAISE EXCEPTION 'lu_site_membership.row_version is server-managed';
    END IF;
    NEW.row_version := OLD.row_version + 1;
    NEW.updated_at := CURRENT_TIMESTAMP;
  END IF;
  RETURN NEW;
END
$func$;

CREATE OR REPLACE FUNCTION public.lu_site_membership_immutable_created()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  IF TG_OP = 'UPDATE' THEN
    IF NEW.created_at IS DISTINCT FROM OLD.created_at THEN
      RAISE EXCEPTION 'lu_site_membership.created_at is immutable';
    END IF;
    IF NEW.created_by_user_id IS DISTINCT FROM OLD.created_by_user_id THEN
      RAISE EXCEPTION 'lu_site_membership.created_by_user_id is immutable';
    END IF;
  END IF;
  RETURN NEW;
END
$func$;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname = 'trg_lu_site_membership_row_version_inc') THEN
    CREATE TRIGGER trg_lu_site_membership_row_version_inc
      BEFORE INSERT OR UPDATE ON public.lu_site_membership
      FOR EACH ROW EXECUTE FUNCTION public.lu_site_membership_row_version_inc();
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname = 'trg_lu_site_membership_immutable_created') THEN
    CREATE TRIGGER trg_lu_site_membership_immutable_created
      BEFORE UPDATE ON public.lu_site_membership
      FOR EACH ROW EXECUTE FUNCTION public.lu_site_membership_immutable_created();
  END IF;
END
$do$;

REVOKE EXECUTE ON FUNCTION public.lu_site_membership_row_version_inc() FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION public.lu_site_membership_immutable_created() FROM PUBLIC;

-- Hard deletion of identity roots is forbidden independently of FK coverage.
CREATE OR REPLACE FUNCTION public.lu_identity_hard_delete_forbidden()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, public
AS $func$
BEGIN
  RAISE EXCEPTION 'hard delete is forbidden for identity table %', TG_TABLE_NAME;
END
$func$;

CREATE TRIGGER trg_lu_user_no_hard_delete
  BEFORE DELETE ON public.lu_user
  FOR EACH ROW EXECUTE FUNCTION public.lu_identity_hard_delete_forbidden();

CREATE TRIGGER trg_lu_site_membership_no_hard_delete
  BEFORE DELETE ON public.lu_site_membership
  FOR EACH ROW EXECUTE FUNCTION public.lu_identity_hard_delete_forbidden();

REVOKE EXECUTE ON FUNCTION public.lu_identity_hard_delete_forbidden() FROM PUBLIC;

-- ============================================================================
-- Step 19: lu_session.purpose.
-- ============================================================================

ALTER TABLE public.lu_session
  ADD COLUMN IF NOT EXISTS purpose text;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_session_purpose') THEN
    ALTER TABLE public.lu_session
      ADD CONSTRAINT ck_lu_session_purpose
      CHECK (purpose IS NULL OR purpose IN ('normal', 'password_change', 'site_selection'));
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_session_purpose_site') THEN
    ALTER TABLE public.lu_session
      ADD CONSTRAINT ck_lu_session_purpose_site
      CHECK (purpose IS NULL OR purpose = 'normal' OR active_site_id IS NULL);
  END IF;
END
$do$;

ALTER TABLE public.lu_session
  ALTER COLUMN purpose SET DEFAULT 'normal';

UPDATE public.lu_session
   SET purpose = 'normal'
 WHERE purpose IS NULL;

ALTER TABLE public.lu_session
  ALTER COLUMN purpose SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_lu_session_purpose_expiry
  ON public.lu_session (purpose, absolute_expires_at)
  WHERE revoked_at IS NULL;

-- ============================================================================
-- Step 20: lu_tenant_route.migration_secret_reference (separate from
-- runtime_secret_reference). Server-side resolution only.
-- ============================================================================

ALTER TABLE public.lu_tenant_route
  ADD COLUMN IF NOT EXISTS migration_secret_reference text;

DO $do$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conname = 'ck_lu_tenant_route_migration_secret_reference') THEN
    ALTER TABLE public.lu_tenant_route
      ADD CONSTRAINT ck_lu_tenant_route_migration_secret_reference
      CHECK (migration_secret_reference IS NULL OR (
        migration_secret_reference ~ '^[A-Za-z][A-Za-z0-9._/-]{2,127}$'
        AND migration_secret_reference !~ '\.\.'
      ));
  END IF;
END
$do$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_tenant_route_migration_secret_reference
  ON public.lu_tenant_route (migration_secret_reference)
  WHERE migration_secret_reference IS NOT NULL;

-- ============================================================================
-- Step 21: Tighten runtime grants. lu_auth_runtime already passed the
-- preflight check above. All grants use direct static GRANT/REVOKE syntax;
-- no dynamic SQL is used.
-- ============================================================================

DO $grant$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime') THEN
    RAISE EXCEPTION 'lu_auth_runtime role must exist before applying 0006';
  END IF;
END
$grant$;

-- lu_user: SELECT plus column-level INSERT/UPDATE.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_user FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_user TO lu_auth_runtime;
-- is_super_admin remains writable by this server-only role because F3's
-- privileged global command uses the same database principal; HTTP policy is
-- still required and the general user DTO never exposes this field.
GRANT INSERT (id, email, password_hash, password_scheme, must_change_password, is_super_admin, account_status, status, username, first_name, last_name, second_last_name, identity_card, phone_number, profile_picture_key, password_migrated_at, created_by_user_id, modified_by_user_id) ON TABLE public.lu_user TO lu_auth_runtime;
GRANT UPDATE (email, first_name, last_name, second_last_name, identity_card, phone_number, profile_picture_key, password_hash, password_scheme, must_change_password, password_migrated_at, account_status, status, is_super_admin, security_version, modified_by_user_id) ON TABLE public.lu_user TO lu_auth_runtime;

-- lu_site_membership: SELECT plus column-level INSERT/UPDATE.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_site_membership FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_site_membership TO lu_auth_runtime;
GRANT INSERT (user_id, site_id, role, status, valid_from, valid_until, position, department, hire_date, created_by_user_id, modified_by_user_id) ON TABLE public.lu_site_membership TO lu_auth_runtime;
GRANT UPDATE (role, status, valid_from, valid_until, position, department, hire_date, modified_by_user_id) ON TABLE public.lu_site_membership TO lu_auth_runtime;

-- lu_session: SELECT, INSERT, restricted UPDATE.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_session FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_session TO lu_auth_runtime;
GRANT INSERT ON TABLE public.lu_session TO lu_auth_runtime;
GRANT UPDATE (last_seen_at, idle_expires_at, absolute_expires_at, revoked_at, revocation_reason, active_site_id) ON TABLE public.lu_session TO lu_auth_runtime;

-- lu_login_identifier: SELECT only.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_login_identifier FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_login_identifier TO lu_auth_runtime;
GRANT EXECUTE ON FUNCTION public.lu_login_identifier_normalize(text) TO lu_auth_runtime;

-- lu_legacy_user_xref: SELECT only (crosswalk is migration-owned; runtime is never DML).
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_legacy_user_xref FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_legacy_user_xref TO lu_auth_runtime;

-- lu_identity_migration_state: SELECT only.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_identity_migration_state FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_identity_migration_state TO lu_auth_runtime;

-- lu_identity_audit_event: SELECT + INSERT only (UPDATE/DELETE blocked by trigger).
REVOKE UPDATE, DELETE, TRUNCATE ON TABLE public.lu_identity_audit_event FROM lu_auth_runtime;
GRANT SELECT, INSERT ON TABLE public.lu_identity_audit_event TO lu_auth_runtime;

-- lu_migration_history: SELECT only.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_migration_history FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_migration_history TO lu_auth_runtime;

-- lu_tenant_route: the application runtime needs the runtime reference and
-- writer label for server-side routing. The migration credential reference is
-- intentionally excluded and is read only by the migration connection.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_tenant_route FROM lu_auth_runtime;
GRANT SELECT (site_id, runtime_secret_reference, writer_label, state, schema_version, last_health_at, created_at, updated_at) ON TABLE public.lu_tenant_route TO lu_auth_runtime;

COMMIT;
