import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';

import { ApiService } from '../../core/api/api.service';
import type { LogEntry, LogSource } from '../../core/api/api.types';

@Component({
    selector: 'app-logs',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        DatePipe,
        ReactiveFormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatSelectModule,
        MatButtonModule,
        MatTableModule,
        MatProgressSpinnerModule,
        MatIconModule,
        TranslateModule,
    ],
    templateUrl: './logs.component.html',
    styleUrl: './logs.component.scss',
})
export class LogsComponent implements OnInit
{
    private readonly api = inject(ApiService);
    private readonly fb = inject(FormBuilder);

    readonly loading = signal(true);
    readonly logs = signal<LogEntry[]>([]);
    readonly displayedColumns = ['timestamp', 'level', 'source', 'message'];

    readonly filterForm: FormGroup;

    constructor()
    {
        this.filterForm = this.fb.group({
            source: [''],
            limit: [100],
        });
    }

    ngOnInit(): void
    {
        this.refresh();
    }

    refresh(): void
    {
        this.loading.set(true);
        const source = this.filterForm.get('source')?.value as LogSource | '';
        const limit = this.filterForm.get('limit')?.value as number ?? 100;

        this.api.getLogs(source || undefined, limit).subscribe({
            next: (l) =>
            {
                this.logs.set(l);
                this.loading.set(false);
            },
            error: () => this.loading.set(false),
        });
    }

    truncate(text: string, len: number): string
    {
        return text.length > len ? text.substring(0, len) + '…' : text;
    }
}
