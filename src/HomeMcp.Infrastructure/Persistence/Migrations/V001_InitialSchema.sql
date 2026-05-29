CREATE TABLE IF NOT EXISTS users (
    id           TEXT PRIMARY KEY,
    display_name TEXT NOT NULL,
    locale       TEXT NOT NULL DEFAULT 'ru-RU',
    created_at   INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS devices (
    id              TEXT PRIMARY KEY,
    primary_user_id TEXT NOT NULL REFERENCES users(id),
    is_shared       INTEGER NOT NULL DEFAULT 0,
    location        TEXT,
    token_hash      TEXT NOT NULL,
    capabilities    TEXT NOT NULL,
    paired_at       INTEGER NOT NULL,
    last_seen_at    INTEGER
);

CREATE TABLE IF NOT EXISTS sessions (
    id                        TEXT PRIMARY KEY,
    user_id                   TEXT NOT NULL REFERENCES users(id),
    device_id                 TEXT NOT NULL REFERENCES devices(id),
    locale                    TEXT NOT NULL DEFAULT 'ru-RU',
    started_at                INTEGER NOT NULL,
    last_active_at            INTEGER NOT NULL,
    summary                   TEXT,
    summary_until_message_id  INTEGER
);

CREATE INDEX IF NOT EXISTS ix_sessions_user_device
    ON sessions(user_id, device_id, last_active_at DESC);

CREATE TABLE IF NOT EXISTS messages (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id  TEXT NOT NULL REFERENCES sessions(id),
    role        TEXT NOT NULL,
    content     TEXT NOT NULL,
    tool_calls  TEXT,
    created_at  INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_messages_session
    ON messages(session_id, id);

CREATE TABLE IF NOT EXISTS facts (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id    TEXT NOT NULL REFERENCES users(id),
    scope      TEXT NOT NULL,
    key        TEXT NOT NULL,
    value      TEXT NOT NULL,
    source     TEXT,
    created_at INTEGER NOT NULL,
    UNIQUE(user_id, scope, key)
);

CREATE INDEX IF NOT EXISTS ix_facts_user
    ON facts(user_id, scope);

CREATE TABLE IF NOT EXISTS audit_log (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    event_type TEXT NOT NULL,
    actor_id   TEXT,
    target_id  TEXT,
    detail     TEXT,
    created_at INTEGER NOT NULL
);
