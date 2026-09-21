BEGIN;

-- MIG-F3-AUTH-SECURITY-014
-- Distributed login throttling and append-only security audit for the local
-- identity control plane. No seed, role change, destructive DDL or business DML.

SELECT pg_advisory_xact_lock(hashtext('lu:identity-control-plane:0002'));

CREATE TABLE IF NOT EXISTS lu_auth_rate_limit (
    scope text NOT NULL,
    key_hash char(64) NOT NULL,
    attempt_count integer NOT NULL,
    window_started_at timestamptz NOT NULL,
    reset_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_lu_auth_rate_limit PRIMARY KEY (scope, key_hash),
    CONSTRAINT ck_lu_auth_rate_limit_scope
        CHECK (scope IN ('email_ip', 'ip')),
    CONSTRAINT ck_lu_auth_rate_limit_key_hash
        CHECK (key_hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT ck_lu_auth_rate_limit_attempt_count
        CHECK (attempt_count >= 1),
    CONSTRAINT ck_lu_auth_rate_limit_window
        CHECK (reset_at > window_started_at),
    CONSTRAINT ck_lu_auth_rate_limit_updated
        CHECK (updated_at >= window_started_at)
);

CREATE INDEX IF NOT EXISTS ix_lu_auth_rate_limit_reset
    ON lu_auth_rate_limit (reset_at);

CREATE TABLE IF NOT EXISTS lu_security_event (
    id uuid NOT NULL,
    event_type text NOT NULL,
    user_id uuid NULL,
    site_id uuid NULL,
    subject_hash char(64) NULL,
    ip_hash char(64) NULL,
    occurred_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT pk_lu_security_event PRIMARY KEY (id),
    CONSTRAINT fk_lu_security_event_user
        FOREIGN KEY (user_id) REFERENCES lu_user (id) ON DELETE RESTRICT,
    CONSTRAINT fk_lu_security_event_site
        FOREIGN KEY (site_id) REFERENCES lu_site (id) ON DELETE RESTRICT,
    CONSTRAINT ck_lu_security_event_type
        CHECK (event_type IN (
            'login_success',
            'login_failure',
            'login_rate_limited',
            'logout',
            'active_site_changed',
            'admin_bootstrap'
        )),
    CONSTRAINT ck_lu_security_event_subject_hash
        CHECK (subject_hash IS NULL OR subject_hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT ck_lu_security_event_ip_hash
        CHECK (ip_hash IS NULL OR ip_hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT ck_lu_security_event_subject
        CHECK (user_id IS NOT NULL OR subject_hash IS NOT NULL),
    CONSTRAINT ck_lu_security_event_metadata
        CHECK (jsonb_typeof(metadata) = 'object')
);

CREATE INDEX IF NOT EXISTS ix_lu_security_event_type_time
    ON lu_security_event (event_type, occurred_at);

CREATE INDEX IF NOT EXISTS ix_lu_security_event_user_time
    ON lu_security_event (user_id, occurred_at)
    WHERE user_id IS NOT NULL;

COMMIT;
