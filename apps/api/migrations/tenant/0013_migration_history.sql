-- MIG-001-F2-W2: tenant-side migration history ledger (0013).
--
-- Tenant databases host the same per-stream lu_migration_history table as the
-- control plane. The runner backfills immutable pin rows for 0001-0013 inside
-- the same transaction before this migration commits. Future migrations append
-- new ordinals under the tenant stream.
--
-- This migration is purely additive and creates exactly one new table.

BEGIN;

SELECT pg_advisory_xact_lock(hashtext('lu:tenant:0013-migration-history'));

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

-- Tighten runtime grants when the role is present (fail-closed otherwise).
-- Static GRANT/REVOKE only; no dynamic SQL is used.
DO $grant$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime') THEN
    RAISE EXCEPTION 'lu_auth_runtime role must exist before applying 0013';
  END IF;
END
$grant$;

REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.lu_migration_history FROM lu_auth_runtime;
GRANT SELECT ON TABLE public.lu_migration_history TO lu_auth_runtime;

COMMIT;
