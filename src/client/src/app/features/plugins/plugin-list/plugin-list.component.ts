import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ApiService } from '../../../core/api/api.service';
import type { PluginInfo } from '../../../core/api/api.types';
import { LocaleService } from '../../../core/services/locale.service';

@Component({
    selector: 'app-plugin-list',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        RouterLink,
        MatCardModule,
        MatListModule,
        MatIconModule,
        MatButtonModule,
        MatChipsModule,
        MatProgressSpinnerModule,
        TranslateModule,
    ],
    templateUrl: './plugin-list.component.html',
    styleUrl: './plugin-list.component.scss',
})
export class PluginListComponent implements OnInit
{
    private readonly api = inject(ApiService);
    readonly locale = inject(LocaleService);

    readonly loading = signal(true);
    readonly plugins = signal<PluginInfo[]>([]);

    ngOnInit(): void
    {
        this.api.listPlugins().subscribe({
            next: (p) =>
            {
                this.plugins.set(p);
                this.loading.set(false);
            },
            error: () => this.loading.set(false),
        });
    }
}
