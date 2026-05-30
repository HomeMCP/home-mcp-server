import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import type {
    HealthStatus,
    LogEntry,
    LogSource,
    PluginConfigValues,
    PluginInfo,
    SetupInitRequest,
    SetupStatus,
} from './api.types';

@Injectable({ providedIn: 'root' })
export class ApiService
{
    private readonly http = inject(HttpClient);
    private readonly base = '/admin';

    health(): Observable<HealthStatus>
    {
        return this.http.get<HealthStatus>(`${this.base}/health`);
    }

    setupStatus(): Observable<SetupStatus>
    {
        return this.http.get<SetupStatus>(`${this.base}/setup/status`);
    }

    setupInit(req: SetupInitRequest): Observable<{ ok: boolean }>
    {
        return this.http.post<{ ok: boolean }>(`${this.base}/setup/init`, req);
    }

    listPlugins(): Observable<PluginInfo[]>
    {
        return this.http.get<PluginInfo[]>(`${this.base}/plugins`);
    }

    getPlugin(id: string): Observable<PluginInfo>
    {
        return this.http.get<PluginInfo>(`${this.base}/plugins/${id}`);
    }

    savePluginConfig(id: string, config: PluginConfigValues): Observable<PluginInfo>
    {
        return this.http.put<PluginInfo>(`${this.base}/plugins/${id}/config`, { config });
    }

    getLogs(source?: LogSource, limit = 100): Observable<LogEntry[]>
    {
        const params: Record<string, string> = { limit: String(limit) };
        if (source)
        {
            params['source'] = source;
        }
        return this.http.get<LogEntry[]>(`${this.base}/logs`, { params });
    }
}
