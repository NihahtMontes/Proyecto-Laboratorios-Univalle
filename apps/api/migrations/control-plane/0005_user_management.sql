BEGIN;

-- MIG-F4-CP-USERS-001
-- User administration is an additive capability over the existing local
-- identity tables. Runtime can manage accounts and memberships only through
-- the authenticated API; it never receives DDL or a browser-supplied tenant.
SELECT pg_catalog.pg_advisory_xact_lock(
    pg_catalog.hashtext('lu:identity-control-plane:0005-users')
);

DO $grant$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime') THEN
        GRANT SELECT, INSERT, UPDATE ON TABLE public.lu_user, public.lu_site_membership TO lu_auth_runtime;
    END IF;
END
$grant$;

COMMIT;
