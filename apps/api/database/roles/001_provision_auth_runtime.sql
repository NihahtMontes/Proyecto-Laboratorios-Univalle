-- MIG-F3-AUTH-PGQA-012B
-- Provisions the two-role PostgreSQL 18 transport used by Auth in the local
-- GastroExample sandbox. This artifact contains no password and leaves the
-- login principal disabled. A deployment secret must enable lu_auth_login
-- outside this script; application queries always SET LOCAL ROLE to the
-- privilege-bearing, non-login lu_auth_runtime role.
--
-- Scope:
--   * lu_auth_runtime: NOLOGIN, NOINHERIT, exact control-plane ACL.
--   * lu_auth_login: NOLOGIN, NOINHERIT, no object ACL, SET-only membership.
--   * public control-plane tables: no PUBLIC ACL.
--   * public schema: PUBLIC cannot CREATE.
--   * GastroExample database: PUBLIC cannot create temporary objects.
--
-- Operational rollback (deliberately no DROP): disable both roles, revoke the
-- membership, revoke their object ACL, and review PUBLIC hardening separately.
-- Execute only as PostgreSQL superuser. Any failed post-condition rolls back.

BEGIN;

SELECT pg_catalog.pg_advisory_xact_lock(
    pg_catalog.hashtext('lu:roles:provision-auth-runtime:001')
);

SET LOCAL search_path TO pg_catalog, public;

DO $preflight$
BEGIN
    IF pg_catalog.current_database() <> 'GastroExample' THEN
        RAISE EXCEPTION 'role provisioning is restricted to GastroExample';
    END IF;

    IF pg_catalog.current_setting('is_superuser') <> 'on' THEN
        RAISE EXCEPTION 'role provisioning requires a PostgreSQL superuser';
    END IF;

    IF pg_catalog.to_regclass('public.lu_site') IS NULL
       OR pg_catalog.to_regclass('public.lu_user') IS NULL
       OR pg_catalog.to_regclass('public.lu_site_membership') IS NULL
       OR pg_catalog.to_regclass('public.lu_session') IS NULL THEN
        RAISE EXCEPTION 'identity control-plane baseline 0001 is incomplete';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime') THEN
        CREATE ROLE lu_auth_runtime;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_login') THEN
        CREATE ROLE lu_auth_login;
    END IF;
END
$preflight$;

ALTER ROLE lu_auth_runtime WITH
    NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE
    NOREPLICATION NOBYPASSRLS PASSWORD NULL;
ALTER ROLE lu_auth_login WITH
    NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE
    NOREPLICATION NOBYPASSRLS PASSWORD NULL;

ALTER ROLE lu_auth_runtime SET search_path TO pg_catalog, public;
ALTER ROLE lu_auth_login SET search_path TO pg_catalog, public;

-- Reconcile role membership to a single SET-only edge.
REVOKE lu_auth_runtime FROM lu_auth_login;
GRANT lu_auth_runtime TO lu_auth_login
    WITH ADMIN FALSE, INHERIT FALSE, SET TRUE;

-- Harden PUBLIC on the new control-plane surface. CONNECT is intentionally
-- unchanged. TEMP is revoked for this sandbox to prevent temporary-table
-- shadowing; repository SQL is schema-qualified as a second layer.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE ALL PRIVILEGES
    ON TABLE public.lu_site, public.lu_user,
             public.lu_site_membership, public.lu_session
    FROM PUBLIC;
DO $database_acl$
BEGIN
    EXECUTE pg_catalog.format(
        'REVOKE TEMPORARY ON DATABASE %I FROM PUBLIC',
        pg_catalog.current_database()
    );
END
$database_acl$;

-- Remove all direct object ACL before rebuilding the exact whitelist.
REVOKE ALL PRIVILEGES ON SCHEMA public FROM lu_auth_runtime, lu_auth_login;
REVOKE ALL PRIVILEGES
    ON TABLE public.lu_site, public.lu_user,
             public.lu_site_membership, public.lu_session
    FROM lu_auth_runtime, lu_auth_login;

GRANT USAGE ON SCHEMA public TO lu_auth_runtime;
GRANT SELECT
    ON TABLE public.lu_site, public.lu_user,
             public.lu_site_membership, public.lu_session
    TO lu_auth_runtime;

GRANT INSERT
    (id, token_hash, user_id, active_site_id, security_version,
     created_at, last_seen_at, idle_expires_at, absolute_expires_at)
    ON public.lu_session TO lu_auth_runtime;

GRANT UPDATE
    (active_site_id, last_seen_at, idle_expires_at,
     revoked_at, revocation_reason)
    ON public.lu_session TO lu_auth_runtime;

-- PostgreSQL locking clauses require UPDATE on at least one column. This
-- deliberately harmless column grant permits row locks without allowing role,
-- status, password, membership, or security-version changes.
GRANT UPDATE (updated_at)
    ON public.lu_site, public.lu_user, public.lu_site_membership
    TO lu_auth_runtime;

REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER, MAINTAIN
    ON TABLE public.lu_site, public.lu_user,
             public.lu_site_membership, public.lu_session
    FROM lu_auth_runtime, lu_auth_login;

DO $postconditions$
DECLARE
    runtime_oid oid := 'lu_auth_runtime'::pg_catalog.regrole;
    login_oid oid := 'lu_auth_login'::pg_catalog.regrole;
    table_name text;
    column_name text;
    expected_insert boolean;
    expected_update boolean;
    role_row pg_catalog.pg_authid%ROWTYPE;
BEGIN
    -- Absolute role attributes and password-free disabled login state.
    FOR role_row IN
        SELECT * FROM pg_catalog.pg_authid
        WHERE rolname IN ('lu_auth_runtime', 'lu_auth_login')
    LOOP
        IF role_row.rolcanlogin OR role_row.rolsuper OR role_row.rolinherit
           OR role_row.rolcreatedb OR role_row.rolcreaterole
           OR role_row.rolreplication OR role_row.rolbypassrls
           OR role_row.rolpassword IS NOT NULL THEN
            RAISE EXCEPTION 'role attributes outside the approved baseline: %', role_row.rolname;
        END IF;
    END LOOP;

    IF (SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles
        WHERE rolname IN ('lu_auth_runtime', 'lu_auth_login')) <> 2 THEN
        RAISE EXCEPTION 'both auth roles must exist';
    END IF;

    -- Runtime inherits no role. Login has exactly one membership, with SET
    -- enabled and both ADMIN and INHERIT disabled. Runtime has no other member.
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members WHERE member = runtime_oid) THEN
        RAISE EXCEPTION 'lu_auth_runtime must not be a member of another role';
    END IF;
    IF (SELECT pg_catalog.count(*) FROM pg_catalog.pg_auth_members WHERE member = login_oid) <> 1
       OR NOT EXISTS (
           SELECT 1 FROM pg_catalog.pg_auth_members
           WHERE roleid = runtime_oid AND member = login_oid
             AND admin_option = false AND inherit_option = false AND set_option = true
       )
       OR EXISTS (
           SELECT 1 FROM pg_catalog.pg_auth_members
           WHERE roleid = runtime_oid AND member <> login_oid
       ) THEN
        RAISE EXCEPTION 'auth role membership differs from the SET-only contract';
    END IF;

    -- Neither role may own a database, schema, relation, or routine.
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datdba IN (runtime_oid, login_oid))
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspowner IN (runtime_oid, login_oid))
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_class WHERE relowner IN (runtime_oid, login_oid))
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_proc WHERE proowner IN (runtime_oid, login_oid)) THEN
        RAISE EXCEPTION 'auth roles must not own database objects';
    END IF;

    -- PUBLIC hardening required by this sandbox contract.
    IF pg_catalog.has_schema_privilege('public', 'public', 'CREATE')
       OR pg_catalog.has_database_privilege(
           'public', pg_catalog.current_database(), 'TEMPORARY'
       ) THEN
        RAISE EXCEPTION 'PUBLIC retains CREATE or TEMPORARY';
    END IF;

    FOREACH table_name IN ARRAY ARRAY[
        'public.lu_site', 'public.lu_user',
        'public.lu_site_membership', 'public.lu_session'
    ]
    LOOP
        IF pg_catalog.has_table_privilege('public', table_name,
            'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN') THEN
            RAISE EXCEPTION 'PUBLIC retains table privilege on %', table_name;
        END IF;

        IF NOT pg_catalog.has_table_privilege('lu_auth_runtime', table_name, 'SELECT')
           OR pg_catalog.has_table_privilege('lu_auth_runtime', table_name,
              'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN')
           OR pg_catalog.has_table_privilege('lu_auth_login', table_name,
              'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN') THEN
            RAISE EXCEPTION 'table-level ACL differs from whitelist on %', table_name;
        END IF;

        FOR column_name IN
            SELECT a.attname
            FROM pg_catalog.pg_attribute a
            WHERE a.attrelid = table_name::pg_catalog.regclass
              AND a.attnum > 0 AND NOT a.attisdropped
        LOOP
            expected_insert := table_name = 'public.lu_session'
                AND column_name = ANY (ARRAY[
                    'id','token_hash','user_id','active_site_id','security_version',
                    'created_at','last_seen_at','idle_expires_at','absolute_expires_at'
                ]);
            expected_update :=
                (table_name = 'public.lu_session' AND column_name = ANY (ARRAY[
                    'active_site_id','last_seen_at','idle_expires_at',
                    'revoked_at','revocation_reason'
                ]))
                OR (table_name <> 'public.lu_session' AND column_name = 'updated_at');

            IF pg_catalog.has_column_privilege(
                   'lu_auth_runtime', table_name, column_name, 'INSERT'
               ) <> expected_insert
               OR pg_catalog.has_column_privilege(
                   'lu_auth_runtime', table_name, column_name, 'UPDATE'
               ) <> expected_update
               OR pg_catalog.has_column_privilege(
                   'lu_auth_login', table_name, column_name, 'INSERT,UPDATE'
               ) THEN
                RAISE EXCEPTION 'column ACL differs from whitelist: %.%', table_name, column_name;
            END IF;
        END LOOP;
    END LOOP;

    IF NOT pg_catalog.has_schema_privilege('lu_auth_runtime', 'public', 'USAGE')
       OR pg_catalog.has_schema_privilege('lu_auth_runtime', 'public', 'CREATE')
       OR pg_catalog.has_schema_privilege('lu_auth_login', 'public', 'CREATE') THEN
        RAISE EXCEPTION 'schema ACL differs from whitelist';
    END IF;

    -- No direct grant option on schema/table/column ACL for either role.
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        CROSS JOIN LATERAL pg_catalog.aclexplode(c.relacl) a
        WHERE c.oid = ANY (ARRAY[
            'public.lu_site'::pg_catalog.regclass,
            'public.lu_user'::pg_catalog.regclass,
            'public.lu_site_membership'::pg_catalog.regclass,
            'public.lu_session'::pg_catalog.regclass
        ])
          AND a.grantee IN (runtime_oid, login_oid) AND a.is_grantable
    ) OR EXISTS (
        SELECT 1
        FROM pg_catalog.pg_namespace n
        CROSS JOIN LATERAL pg_catalog.aclexplode(n.nspacl) a
        WHERE n.nspname = 'public'
          AND a.grantee IN (runtime_oid, login_oid) AND a.is_grantable
    ) OR EXISTS (
        SELECT 1
        FROM pg_catalog.pg_attribute att
        CROSS JOIN LATERAL pg_catalog.aclexplode(att.attacl) a
        WHERE att.attrelid = ANY (ARRAY[
            'public.lu_site'::pg_catalog.regclass,
            'public.lu_user'::pg_catalog.regclass,
            'public.lu_site_membership'::pg_catalog.regclass,
            'public.lu_session'::pg_catalog.regclass
        ])
          AND a.grantee IN (runtime_oid, login_oid) AND a.is_grantable
    ) THEN
        RAISE EXCEPTION 'auth roles must not hold grant options';
    END IF;

    -- Default EXECUTE for PUBLIC makes any SECURITY DEFINER routine in public
    -- security-sensitive. Fail instead of silently inheriting that surface.
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_proc p
        JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'public' AND p.prosecdef
          AND (
              pg_catalog.has_function_privilege('lu_auth_runtime', p.oid, 'EXECUTE')
              OR pg_catalog.has_function_privilege('lu_auth_login', p.oid, 'EXECUTE')
          )
    ) THEN
        RAISE EXCEPTION 'executable SECURITY DEFINER routine found in public';
    END IF;
END
$postconditions$;

COMMIT;
