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
import type { ConfigFieldDescriptor, PluginInfo } from '../../../core/api/api.types';
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
    private readonly route = inject(ActivatedRoute);
    private readonly api = inject(ApiService);
    private readonly fb = inject(FormBuilder);
    readonly locale = inject(LocaleService);

    readonly loading = signal(true);
    readonly plugin = signal<PluginInfo | null>(null);

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
                this.configForm = this.buildConfigForm(p.configSchema);
                this.loading.set(false);
            },
            error: () => this.loading.set(false),
        });
    }

    saveConfig(): void
    {
        // TODO: wire up PUT /admin/plugins/{id}/config when backend endpoint is added
        console.warn('Config save not yet implemented:', this.configForm.value);
    }

    private buildConfigForm(schema: ConfigFieldDescriptor[]): FormGroup
    {
        const controls: Record<string, FormControl> = {};
        for (const field of schema)
        {
            const validators = field.required ? [Validators.required] : [];
            controls[field.key] = new FormControl(field.defaultValue ?? '', validators);
        }
        return this.fb.group(controls);
    }
}
