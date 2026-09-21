BEGIN;

-- MIG-F4-ROUTE-CATALOG-008
-- Server-side tenant routing metadata. Secret values never live in this table;
-- runtime_secret_reference identifies a value held by the deployment secret manager.

SELECT pg_advisory_xact_lock(hashtext('lu:identity-control-plane:0003'));

CREATE TABLE IF NOT EXISTS lu_tenant_route (
    site_id uuid NOT NULL,
    runtime_secret_reference text NOT NULL,
    writer_label text NOT NULL,
    state text NOT NULL DEFAULT 'migrating',
    schema_version integer NOT NULL DEFAULT 0,
    last_health_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_lu_tenant_route PRIMARY KEY (site_id),
    CONSTRAINT fk_lu_tenant_route_site
        FOREIGN KEY (site_id) REFERENCES lu_site (id) ON DELETE RESTRICT,
    CONSTRAINT ck_lu_tenant_route_secret_reference
        CHECK (
            runtime_secret_reference ~ '^[A-Za-z][A-Za-z0-9._/-]{2,127}$'
            AND runtime_secret_reference !~ '\.\.'
        ),
    CONSTRAINT ck_lu_tenant_route_writer_label
        CHECK (writer_label IN ('LEGACY_SQLSERVER', 'NEST_SQLSERVER', 'NEST_POSTGRES')),
    CONSTRAINT ck_lu_tenant_route_state
        CHECK (state IN ('active', 'migrating', 'degraded', 'disabled')),
    CONSTRAINT ck_lu_tenant_route_schema_version
        CHECK (schema_version >= 0),
    CONSTRAINT ck_lu_tenant_route_updated
        CHECK (updated_at >= created_at)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_tenant_route_secret_reference
    ON lu_tenant_route (runtime_secret_reference);

CREATE INDEX IF NOT EXISTS ix_lu_tenant_route_state
    ON lu_tenant_route (state);

COMMIT;
