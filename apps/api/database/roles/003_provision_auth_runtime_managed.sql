-- MIG-F4-MANAGED-ROLE-004
-- Reconciles least-privilege Auth roles for the managed Neon control plane.
-- This artifact contains no password and deliberately leaves lu_auth_login
-- disabled. Activation is a separate parameterized CLI operation.

BEGIN;

SELECT pg_catalog.pg_advisory_xact_lock(
    pg_catalog.hashtext('lu:roles:provision-auth-runtime-managed:003')
);

SET LOCAL search_path TO pg_catalog, public;

DO $preflight$
DECLARE
    required_tables constant text[] := ARRAY[
        'lu_site', 'lu_user', 'lu_site_membership', 'lu_session',
        'lu_auth_rate_limit', 'lu_security_event', 'lu_tenant_route'
    ];
    table_name text;
BEGIN
    IF pg_catalog.current_database() <> 'neondb' THEN
        RAISE EXCEPTION 'managed role provisioning is restricted to neondb';
    END IF;
    IF NOT COALESCE((
        SELECT r.rolcreaterole FROM pg_catalog.pg_roles r
        WHERE r.rolname = CURRENT_USER
    ), false) THEN
        RAISE EXCEPTION 'managed role provisioning requires CREATEROLE';
    END IF;
    FOREACH table_name IN ARRAY required_tables LOOP
        IF pg_catalog.to_regclass('public.' || table_name) IS NULL THEN
            RAISE EXCEPTION 'required control-plane table is missing: %', table_name;
        END IF;
    END LOOP;

    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime') THEN
        CREATE ROLE lu_auth_runtime;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_login') THEN
        CREATE ROLE lu_auth_login;
    END IF;
END
$preflight$;

ALTER ROLE lu_auth_runtime WITH
    NOLOGIN NOINHERIT NOCREATEDB NOCREATEROLE PASSWORD NULL;
ALTER ROLE lu_auth_login WITH
    NOLOGIN NOINHERIT NOCREATEDB NOCREATEROLE PASSWORD NULL;

-- PostgreSQL installations differ on whether the creator receives explicit
-- administration membership. Make the approved administrative edge deterministic.
GRANT lu_auth_runtime TO CURRENT_USER
    WITH ADMIN TRUE, INHERIT FALSE, SET TRUE;

ALTER ROLE lu_auth_runtime SET search_path TO pg_catalog, public;
ALTER ROLE lu_auth_login SET search_path TO pg_catalog, public;

REVOKE lu_auth_runtime FROM lu_auth_login;
GRANT lu_auth_runtime TO lu_auth_login
    WITH ADMIN FALSE, INHERIT FALSE, SET TRUE;

REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE TEMPORARY ON DATABASE neondb FROM PUBLIC;
REVOKE ALL PRIVILEGES ON SCHEMA public FROM lu_auth_runtime, lu_auth_login;
REVOKE ALL PRIVILEGES
    ON TABLE public.lu_site, public.lu_user,
             public.lu_site_membership, public.lu_session,
             public.lu_auth_rate_limit, public.lu_security_event,
             public.lu_tenant_route
    FROM PUBLIC, lu_auth_runtime, lu_auth_login;

GRANT USAGE ON SCHEMA public TO lu_auth_runtime;
GRANT SELECT
    ON TABLE public.lu_site, public.lu_user,
             public.lu_site_membership, public.lu_session,
             public.lu_tenant_route
    TO lu_auth_runtime;

GRANT INSERT
    (id, token_hash, user_id, active_site_id, security_version,
     created_at, last_seen_at, idle_expires_at, absolute_expires_at)
    ON public.lu_session TO lu_auth_runtime;
GRANT UPDATE
    (active_site_id, last_seen_at, idle_expires_at,
     revoked_at, revocation_reason)
    ON public.lu_session TO lu_auth_runtime;
GRANT UPDATE (updated_at)
    ON public.lu_site, public.lu_user, public.lu_site_membership
    TO lu_auth_runtime;

GRANT SELECT, DELETE ON TABLE public.lu_auth_rate_limit TO lu_auth_runtime;
GRANT INSERT
    (scope, key_hash, attempt_count, window_started_at, reset_at, updated_at)
    ON public.lu_auth_rate_limit TO lu_auth_runtime;
GRANT UPDATE
    (attempt_count, window_started_at, reset_at, updated_at)
    ON public.lu_auth_rate_limit TO lu_auth_runtime;
GRANT INSERT
    (id, event_type, user_id, site_id, subject_hash, ip_hash, occurred_at, metadata)
    ON public.lu_security_event TO lu_auth_runtime;

REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER, MAINTAIN
    ON TABLE public.lu_site, public.lu_user,
             public.lu_site_membership, public.lu_session
    FROM lu_auth_runtime, lu_auth_login;
REVOKE UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER, MAINTAIN
    ON TABLE public.lu_security_event FROM lu_auth_runtime, lu_auth_login;
REVOKE TRUNCATE, REFERENCES, TRIGGER, MAINTAIN
    ON TABLE public.lu_auth_rate_limit FROM lu_auth_runtime, lu_auth_login;
REVOKE INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER, MAINTAIN
    ON TABLE public.lu_tenant_route FROM lu_auth_runtime, lu_auth_login;

DO $postconditions$
DECLARE
    runtime_oid oid := 'lu_auth_runtime'::pg_catalog.regrole;
    login_oid oid := 'lu_auth_login'::pg_catalog.regrole;
    role_row record;
    table_name text;
BEGIN
    FOR role_row IN
        SELECT * FROM pg_catalog.pg_roles
        WHERE rolname IN ('lu_auth_runtime', 'lu_auth_login')
    LOOP
        IF role_row.rolcanlogin OR role_row.rolsuper OR role_row.rolinherit
           OR role_row.rolcreatedb OR role_row.rolcreaterole
           OR role_row.rolreplication OR role_row.rolbypassrls THEN
            RAISE EXCEPTION 'role attributes outside baseline: %', role_row.rolname;
        END IF;
    END LOOP;
    IF (SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles
        WHERE rolname IN ('lu_auth_runtime', 'lu_auth_login')) <> 2 THEN
        RAISE EXCEPTION 'both auth roles must exist';
    END IF;

    IF EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members WHERE member = runtime_oid)
       OR (SELECT pg_catalog.count(*) FROM pg_catalog.pg_auth_members
           WHERE member = login_oid) <> 1
       OR NOT EXISTS (
           SELECT 1 FROM pg_catalog.pg_auth_members
           WHERE roleid = runtime_oid AND member = login_oid
             AND admin_option = false AND inherit_option = false AND set_option = true
       )
       OR EXISTS (
           SELECT 1 FROM pg_catalog.pg_auth_members
           WHERE roleid = runtime_oid
             AND member NOT IN (login_oid, CURRENT_USER::pg_catalog.regrole)
       )
       OR NOT EXISTS (
           SELECT 1 FROM pg_catalog.pg_auth_members
           WHERE roleid = runtime_oid
             AND member = CURRENT_USER::pg_catalog.regrole
             AND admin_option = true
       ) THEN
        RAISE EXCEPTION 'auth membership differs from SET-only contract';
    END IF;

    IF EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datdba IN (runtime_oid, login_oid))
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspowner IN (runtime_oid, login_oid))
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_class WHERE relowner IN (runtime_oid, login_oid))
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_proc WHERE proowner IN (runtime_oid, login_oid)) THEN
        RAISE EXCEPTION 'auth roles must not own database objects';
    END IF;

    IF pg_catalog.has_schema_privilege('public', 'public', 'CREATE')
       OR pg_catalog.has_database_privilege('public', 'neondb', 'TEMPORARY')
       OR NOT pg_catalog.has_schema_privilege('lu_auth_runtime', 'public', 'USAGE')
       OR pg_catalog.has_schema_privilege('lu_auth_runtime', 'public', 'CREATE')
       OR pg_catalog.has_schema_privilege('lu_auth_login', 'public', 'CREATE') THEN
        RAISE EXCEPTION 'schema or database ACL differs from baseline';
    END IF;

    FOREACH table_name IN ARRAY ARRAY[
        'public.lu_site', 'public.lu_user', 'public.lu_site_membership',
        'public.lu_session', 'public.lu_auth_rate_limit', 'public.lu_security_event',
        'public.lu_tenant_route'
    ] LOOP
        IF pg_catalog.has_table_privilege(
               'public', table_name,
               'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
           ) OR pg_catalog.has_table_privilege(
               'lu_auth_login', table_name,
               'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
           ) THEN
            RAISE EXCEPTION 'PUBLIC or login retains table privilege on %', table_name;
        END IF;
    END LOOP;

    IF NOT pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_site', 'SELECT')
       OR NOT pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_user', 'SELECT')
       OR NOT pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_site_membership', 'SELECT')
       OR NOT pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_session', 'SELECT')
       OR NOT pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_auth_rate_limit', 'SELECT,DELETE')
       OR NOT pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_tenant_route', 'SELECT')
       OR pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_tenant_route',
           'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN')
       OR pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_security_event',
           'SELECT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN') THEN
        RAISE EXCEPTION 'runtime table ACL differs from whitelist';
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        CROSS JOIN LATERAL pg_catalog.aclexplode(c.relacl) a
        WHERE c.oid = ANY (ARRAY[
            'public.lu_site'::pg_catalog.regclass,
            'public.lu_user'::pg_catalog.regclass,
            'public.lu_site_membership'::pg_catalog.regclass,
            'public.lu_session'::pg_catalog.regclass,
            'public.lu_auth_rate_limit'::pg_catalog.regclass,
            'public.lu_security_event'::pg_catalog.regclass,
            'public.lu_tenant_route'::pg_catalog.regclass
        ]) AND a.grantee IN (runtime_oid, login_oid) AND a.is_grantable
    ) OR EXISTS (
        SELECT 1 FROM pg_catalog.pg_attribute att
        CROSS JOIN LATERAL pg_catalog.aclexplode(att.attacl) a
        WHERE att.attrelid = ANY (ARRAY[
            'public.lu_site'::pg_catalog.regclass,
            'public.lu_user'::pg_catalog.regclass,
            'public.lu_site_membership'::pg_catalog.regclass,
            'public.lu_session'::pg_catalog.regclass,
            'public.lu_auth_rate_limit'::pg_catalog.regclass,
            'public.lu_security_event'::pg_catalog.regclass,
            'public.lu_tenant_route'::pg_catalog.regclass
        ]) AND a.grantee IN (runtime_oid, login_oid) AND a.is_grantable
    ) THEN
        RAISE EXCEPTION 'auth roles must not hold grant options';
    END IF;
END
$postconditions$;

COMMIT;
