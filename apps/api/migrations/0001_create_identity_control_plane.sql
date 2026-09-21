BEGIN;

-- Prevent concurrent executions of this migration in the same database.
SELECT pg_advisory_xact_lock(hashtext('lu:identity-control-plane:0001'));

CREATE TABLE IF NOT EXISTS lu_site
(
    id          uuid        PRIMARY KEY,
    code        text        NOT NULL CHECK (btrim(code) <> ''),
    name        text        NOT NULL CHECK (btrim(name) <> ''),
    status      text        NOT NULL DEFAULT 'provisioning'
                            CHECK (status IN ('provisioning', 'active', 'migrating', 'degraded', 'disabled')),
    created_at  timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (updated_at >= created_at)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_site_code_lower
    ON lu_site (lower(code));

CREATE TABLE IF NOT EXISTS lu_user
(
    id                uuid        PRIMARY KEY,
    email             text        NOT NULL CHECK (btrim(email) <> ''),
    full_name         text        NOT NULL CHECK (btrim(full_name) <> ''),
    password_hash     text        NOT NULL CHECK (btrim(password_hash) <> ''),
    is_super_admin    boolean     NOT NULL DEFAULT false,
    status            text        NOT NULL DEFAULT 'active'
                                  CHECK (status IN ('active', 'disabled')),
    security_version  bigint      NOT NULL DEFAULT 0 CHECK (security_version >= 0),
    created_at        timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at        timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (updated_at >= created_at)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lu_user_email_lower
    ON lu_user (lower(email));

CREATE TABLE IF NOT EXISTS lu_site_membership
(
    user_id      uuid        NOT NULL,
    site_id      uuid        NOT NULL,
    role         text        NOT NULL CHECK (role IN ('Administrador', 'Supervisor')),
    status       text        NOT NULL DEFAULT 'active'
                              CHECK (status IN ('active', 'suspended', 'revoked')),
    valid_from   timestamptz NULL,
    valid_until  timestamptz NULL,
    created_at   timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, site_id),
    CONSTRAINT fk_lu_site_membership_user
        FOREIGN KEY (user_id) REFERENCES lu_user (id) ON DELETE RESTRICT,
    CONSTRAINT fk_lu_site_membership_site
        FOREIGN KEY (site_id) REFERENCES lu_site (id) ON DELETE RESTRICT,
    CHECK (valid_until IS NULL OR valid_from IS NULL OR valid_until > valid_from),
    CHECK (updated_at >= created_at)
);

CREATE INDEX IF NOT EXISTS ix_lu_site_membership_site_status
    ON lu_site_membership (site_id, status);

CREATE TABLE IF NOT EXISTS lu_session
(
    id                   uuid        PRIMARY KEY,
    token_hash           char(64)    NOT NULL UNIQUE
                                     CHECK (token_hash ~ '^[0-9a-f]{64}$'),
    user_id              uuid        NOT NULL,
    active_site_id       uuid        NULL,
    security_version     bigint      NOT NULL CHECK (security_version >= 0),
    created_at           timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_seen_at         timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    idle_expires_at      timestamptz NOT NULL,
    absolute_expires_at  timestamptz NOT NULL,
    revoked_at           timestamptz NULL,
    revocation_reason    text        NULL,
    CONSTRAINT fk_lu_session_user
        FOREIGN KEY (user_id) REFERENCES lu_user (id) ON DELETE RESTRICT,
    CONSTRAINT fk_lu_session_active_site
        FOREIGN KEY (active_site_id) REFERENCES lu_site (id) ON DELETE RESTRICT,
    CHECK (last_seen_at >= created_at),
    CHECK (idle_expires_at > created_at),
    CHECK (absolute_expires_at > created_at),
    CHECK (idle_expires_at <= absolute_expires_at),
    CHECK (revoked_at IS NULL OR revoked_at >= created_at),
    CHECK (revoked_at IS NULL OR revocation_reason IS NOT NULL),
    CHECK (revocation_reason IS NULL OR btrim(revocation_reason) <> '')
);

-- token_hash stores a lowercase SHA-256 digest; raw session and CSRF secrets
-- must never be persisted. Foreign keys intentionally do not cascade: identity
-- history is retained and business records are never hard-deleted by this migration.
CREATE INDEX IF NOT EXISTS ix_lu_session_user
    ON lu_session (user_id);

CREATE INDEX IF NOT EXISTS ix_lu_session_active_expiry
    ON lu_session (absolute_expires_at, idle_expires_at)
    WHERE revoked_at IS NULL;

COMMIT;
