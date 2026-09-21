BEGIN;

-- MIG-F7-PG-MANAGEMENTS-001
-- Managements and their plan entries are tenant-local. Preventive and
-- corrective rows may be active simultaneously, but only one per type.
SELECT pg_catalog.pg_advisory_xact_lock(
    pg_catalog.hashtext('lu:tenant:managements:0006')
);

DO $grant$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'lu_auth_runtime') THEN
        GRANT SELECT, INSERT, UPDATE ON TABLE public.lu_management, public.lu_management_plan TO lu_auth_runtime;
        GRANT USAGE, SELECT, UPDATE ON SEQUENCE public.lu_management_id_seq, public.lu_management_plan_id_seq TO lu_auth_runtime;
    END IF;
END
$grant$;

COMMIT;
