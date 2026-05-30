CREATE TABLE IF NOT EXISTS plugin_runtime_config (
    plugin_id  TEXT NOT NULL,
    key        TEXT NOT NULL,
    value      TEXT NOT NULL,
    updated_at INTEGER NOT NULL,
    PRIMARY KEY (plugin_id, key)
);

CREATE INDEX IF NOT EXISTS ix_plugin_runtime_config_plugin
    ON plugin_runtime_config(plugin_id);
