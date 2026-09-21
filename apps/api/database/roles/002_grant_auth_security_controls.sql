-- MIG-F3-AUTH-SECURITY-014
-- Reconciles minimum runtime ACL for migration 0002. No password, data, role
-- creation, ownership change or destructive DDL. Restricted to GastroExample.

BEGIN;

SELECT pg_catalog.pg_advisory_xact_lock(
    pg_catalog.hashtext('lu:roles:grant-auth-security-controls:002')
);

SET LOCAL search_path TO pg_catalog, public;

DO $preflight$
BEGIN
    IF pg_catalog.current_database() <> 'GastroExample' THEN
        RAISE EXCEPTION 'security-control grants are restricted to GastroExample';
    END IF;
    IF pg_catalog.current_setting('is_superuser') <> 'on' THEN
        RAISE EXCEPTION 'security-control grants require a PostgreSQL superuser';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_login') THEN
        RAISE EXCEPTION 'auth roles must be provisioned before security-control grants';
    END IF;
    IF pg_catalog.to_regclass('public.lu_auth_rate_limit') IS NULL
       OR pg_catalog.to_regclass('public.lu_security_event') IS NULL THEN
        RAISE EXCEPTION 'migration 0002 security-control tables are incomplete';
    END IF;
END
$preflight$;

REVOKE ALL PRIVILEGES
    ON TABLE public.lu_auth_rate_limit, public.lu_security_event
    FROM PUBLIC, lu_auth_runtime, lu_auth_login;

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

REVOKE UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER, MAINTAIN
    ON TABLE public.lu_security_event FROM lu_auth_runtime, lu_auth_login;
REVOKE TRUNCATE, REFERENCES, TRIGGER, MAINTAIN
    ON TABLE public.lu_auth_rate_limit FROM lu_auth_runtime, lu_auth_login;

DO $postconditions$
DECLARE
    runtime_oid oid := 'lu_auth_runtime'::pg_catalog.regrole;
    login_oid oid := 'lu_auth_login'::pg_catalog.regrole;
    column_name text;
    expected_rate_insert boolean;
    expected_rate_update boolean;
BEGIN
    IF pg_catalog.has_table_privilege(
           'public', 'public.lu_auth_rate_limit',
           'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
       )
       OR pg_catalog.has_table_privilege(
           'public', 'public.lu_security_event',
           'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
       ) THEN
        RAISE EXCEPTION 'PUBLIC retains privileges on security-control tables';
    END IF;

    IF NOT pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_auth_rate_limit', 'SELECT,DELETE'
       )
       OR pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_auth_rate_limit',
           'TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
       )
       OR pg_catalog.has_table_privilege(
           'lu_auth_runtime', 'public.lu_security_event',
           'SELECT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
       )
       OR pg_catalog.has_table_privilege(
           'lu_auth_login', 'public.lu_auth_rate_limit',
           'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
       )
       OR pg_catalog.has_table_privilege(
           'lu_auth_login', 'public.lu_security_event',
           'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
       ) THEN
        RAISE EXCEPTION 'table ACL differs from the security-control whitelist';
    END IF;

    FOR column_name IN
        SELECT a.attname FROM pg_catalog.pg_attribute a
        WHERE a.attrelid = 'public.lu_auth_rate_limit'::pg_catalog.regclass
          AND a.attnum > 0 AND NOT a.attisdropped
    LOOP
        expected_rate_insert := column_name = ANY (ARRAY[
            'scope','key_hash','attempt_count','window_started_at',
            'reset_at','updated_at'
        ]);
        expected_rate_update := column_name = ANY (ARRAY[
            'attempt_count','window_started_at','reset_at','updated_at'
        ]);
        IF pg_catalog.has_column_privilege(
               'lu_auth_runtime', 'public.lu_auth_rate_limit', column_name, 'INSERT'
           ) <> expected_rate_insert
           OR pg_catalog.has_column_privilege(
               'lu_auth_runtime', 'public.lu_auth_rate_limit', column_name, 'UPDATE'
           ) <> expected_rate_update THEN
            RAISE EXCEPTION 'rate-limit column ACL differs from whitelist: %', column_name;
        END IF;
    END LOOP;

    FOR column_name IN
        SELECT a.attname FROM pg_catalog.pg_attribute a
        WHERE a.attrelid = 'public.lu_security_event'::pg_catalog.regclass
          AND a.attnum > 0 AND NOT a.attisdropped
    LOOP
        IF NOT pg_catalog.has_column_privilege(
               'lu_auth_runtime', 'public.lu_security_event', column_name, 'INSERT'
           )
           OR pg_catalog.has_column_privilege(
               'lu_auth_runtime', 'public.lu_security_event', column_name, 'UPDATE'
           ) THEN
            RAISE EXCEPTION 'security-event column ACL differs from append-only contract: %',
                column_name;
        END IF;
    END LOOP;

    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        CROSS JOIN LATERAL pg_catalog.aclexplode(c.relacl) a
        WHERE c.oid = ANY (ARRAY[
            'public.lu_auth_rate_limit'::pg_catalog.regclass,
            'public.lu_security_event'::pg_catalog.regclass
        ])
          AND a.grantee IN (runtime_oid, login_oid) AND a.is_grantable
    ) OR EXISTS (
        SELECT 1 FROM pg_catalog.pg_attribute att
        CROSS JOIN LATERAL pg_catalog.aclexplode(att.attacl) a
        WHERE att.attrelid = ANY (ARRAY[
            'public.lu_auth_rate_limit'::pg_catalog.regclass,
            'public.lu_security_event'::pg_catalog.regclass
        ])
          AND a.grantee IN (runtime_oid, login_oid) AND a.is_grantable
    ) THEN
        RAISE EXCEPTION 'auth roles must not hold grant options on security controls';
    END IF;
END
$postconditions$;

COMMIT;
