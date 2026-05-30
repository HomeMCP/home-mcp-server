import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ApiService } from '../../../core/api/api.service';
import type { ConfigFieldDescriptor, PluginConfigValues,PluginInfo } from '../../../core/api/api.types';
import { LocaleService } from '../../../core/services/locale.service';

@Component({
    selector: 'app-plugin-detail',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatCheckboxModule,
        MatButtonModule,
        MatProgressSpinnerModule,
        MatListModule,
        MatIconModule,
        MatTabsModule,
        TranslateModule,
    ],
    templateUrl: './plugin-detail.component.html',
    styleUrl: './plugin-detail.component.scss',
})
export class PluginDetailComponent implements OnInit
{
    private readonly storedPasswordMarker = '__stored__';
    private readonly route = inject(ActivatedRoute);
    private readonly api = inject(ApiService);
    private readonly fb = inject(FormBuilder);
    readonly locale = inject(LocaleService);

    readonly loading = signal(true);
    readonly plugin = signal<PluginInfo | null>(null);
    readonly saving = signal(false);

    /** Not readonly — rebuilt when plugin schema loads. */
    configForm: FormGroup = this.fb.group({});

    ngOnInit(): void
    {
        const id = this.route.snapshot.paramMap.get('id');
        if (!id)
        {
            return;
        }

        this.api.getPlugin(id).subscribe({
            next: (p) =>
            {
                this.plugin.set(p);
                this.configForm = this.buildConfigForm(p.configSchema, p.configValues);
                this.loading.set(false);
            },
            error: () => this.loading.set(false),
        });
    }

    saveConfig(): void
    {
        const current = this.plugin();
        if (!current)
        {
            return;
        }

        if (this.configForm.invalid)
        {
            this.configForm.markAllAsTouched();
            return;
        }

        const config = this.toConfigValues(this.configForm.value);

        this.saving.set(true);
        this.api.savePluginConfig(current.id, config).subscribe({
            next: (updated) =>
            {
                this.plugin.set(updated);
                this.configForm = this.buildConfigForm(updated.configSchema, updated.configValues);
                this.saving.set(false);
            },
            error: () => this.saving.set(false),
        });
    }

    private buildConfigForm(schema: ConfigFieldDescriptor[], values: PluginConfigValues): FormGroup
    {
        const controls: Record<string, FormControl> = {};
        for (const field of schema)
        {
            let validators = field.required ? [Validators.required] : [];
            let value = values?.[field.key];

            if (field.type === 'password' && value === this.storedPasswordMarker)
            {
                validators = [];
                value = '';
            }
            controls[field.key] = new FormControl(
                value ?? field.defaultValue ?? '',
                validators);
        }
        return this.fb.group(controls);
    }

    private toConfigValues(raw: Record<string, unknown>): PluginConfigValues
    {
        const result: PluginConfigValues = {};
        for (const [key, value] of Object.entries(raw))
        {
            if (typeof value === 'boolean' || typeof value === 'number' || typeof value === 'string')
            {
                result[key] = value;
            }
            else
            {
                result[key] = value === null || value === undefined ? null : String(value);
            }
        }
        return result;
    }
}
