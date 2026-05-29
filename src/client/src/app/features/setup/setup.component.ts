import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatStepperModule } from '@angular/material/stepper';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ApiService } from '../../core/api/api.service';

/** Pure function — lives outside the class to avoid any `this` binding concerns. */
function passwordMatchValidator(group: AbstractControl): ValidationErrors | null
{
    const password = group.get('password')?.value as string | undefined;
    const confirm = group.get('confirmPassword')?.value as string | undefined;
    return password && confirm && password !== confirm
        ? { passwordMismatch: true }
        : null;
}

@Component({
    selector: 'app-setup',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        MatStepperModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatButtonModule,
        MatCardModule,
        MatProgressSpinnerModule,
        TranslateModule,
    ],
    templateUrl: './setup.component.html',
    styleUrl: './setup.component.scss',
})
export class SetupComponent
{
    private readonly fb = inject(FormBuilder);
    private readonly api = inject(ApiService);
    private readonly router = inject(Router);

    readonly loading = signal(false);
    readonly error = signal<string | null>(null);

    readonly adminForm: FormGroup;
    readonly serverForm: FormGroup;

    constructor()
    {
        this.adminForm = this.fb.group(
            {
                password: ['', [Validators.required, Validators.minLength(8)]],
                confirmPassword: ['', Validators.required],
            },
            { validators: passwordMatchValidator },
        );

        this.serverForm = this.fb.group({
            serverName: ['Home MCP'],
            locale: ['en-US'],
        });
    }

    complete(): void
    {
        if (!this.adminForm.valid || !this.serverForm.valid)
        {
            return;
        }

        this.loading.set(true);
        this.error.set(null);

        this.api.setupInit({
            password: this.adminForm.get('password')!.value as string,
            serverName: this.serverForm.get('serverName')!.value as string,
            defaultLocale: this.serverForm.get('locale')!.value as string,
        }).subscribe({
            next: () => this.router.navigate(['/dashboard']),
            error: (err: { error?: { error?: string } }) =>
            {
                this.loading.set(false);
                this.error.set(err.error?.error ?? 'setup.error');
            },
        });
    }
}
