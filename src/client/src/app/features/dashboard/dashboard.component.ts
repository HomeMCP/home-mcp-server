import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ApiService } from '../../core/api/api.service';
import type { HealthStatus, LogEntry, PluginInfo } from '../../core/api/api.types';

@Component({
    selector: 'app-dashboard',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        DatePipe,
        RouterLink,
        MatCardModule,
        MatChipsModule,
        MatIconModule,
        MatListModule,
        MatButtonModule,
        MatProgressSpinnerModule,
        TranslateModule,
    ],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit
{
    private readonly api = inject(ApiService);

    readonly health = signal<HealthStatus | null>(null);
    readonly plugins = signal<PluginInfo[] | null>(null);
    readonly logs = signal<LogEntry[] | null>(null);

    ngOnInit(): void
    {
        this.api.health().subscribe(h => this.health.set(h));
        this.api.listPlugins().subscribe(p => this.plugins.set(p));
        this.api.getLogs(undefined, 50).subscribe(l => this.logs.set(l));
    }

    formatUptime(seconds: number): string
    {
        const days = Math.floor(seconds / 86400);
        const hours = Math.floor((seconds % 86400) / 3600);
        const mins = Math.floor((seconds % 3600) / 60);

        if (days > 0)
        {
            return `${days}d ${hours}h`;
        }
        if (hours > 0)
        {
            return `${hours}h ${mins}m`;
        }
        return `${mins}m`;
    }

    logIcon(level: string): string
    {
        const icons: Record<string, string> = {
            error: 'error',
            warning: 'warning',
            information: 'info',
            debug: 'bug_report',
            critical: 'crisis_alert',
        };
        return icons[level] ?? 'info';
    }
}
