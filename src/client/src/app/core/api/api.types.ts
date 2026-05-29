// ── Setup ──────────────────────────────────────────────────────────────────────
export interface SetupStatus
{
    complete: boolean;
    serverName: string;
}

export interface SetupInitRequest
{
    password: string;
    serverName: string;
    defaultLocale: string;
}

// ── Health ─────────────────────────────────────────────────────────────────────
export interface HealthStatus
{
    status: string;
    version: string;
    uptime: number;
}

// ── Plugins ───────────────────────────────────────────────────────────────────
export type ConfigFieldType = 'text' | 'password' | 'url' | 'integer' | 'boolean' | 'select';
export type PluginStatus = 'active' | 'inactive' | 'error';

export interface ConfigFieldDescriptor
{
    key: string;
    type: ConfigFieldType;
    required: boolean;
    labels: Record<string, string>;
    hints: Record<string, string>;
    defaultValue?: string | null;
    options?: string[] | null;
}

export interface ToolInfo
{
    name: string;
    description: string;
}

export interface PluginInfo
{
    id: string;
    displayName: string;
    version: string;
    status: PluginStatus;
    descriptions: Record<string, string>;
    tools: ToolInfo[];
    configSchema: ConfigFieldDescriptor[];
}

// ── Logs ──────────────────────────────────────────────────────────────────────
export type LogLevel = 'debug' | 'information' | 'warning' | 'error' | 'critical';
export type LogSource = 'application' | 'plugin' | 'llm';

export interface LogEntry
{
    timestamp: string;
    level: LogLevel;
    source: LogSource;
    sourceId: string;
    message: string;
}
