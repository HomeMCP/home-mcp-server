import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { LocaleService } from './core/services/locale.service';
import { ThemeService } from './core/services/theme.service';

@Component({
    selector: 'app-root',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet],
    templateUrl: './app.component.html',
})
export class AppComponent implements OnInit
{
    private readonly theme = inject(ThemeService);
    private readonly locale = inject(LocaleService);

    ngOnInit(): void
    {
        // Accessing signals triggers their constructor effects (theme + locale apply)
        void this.theme.theme();
        void this.locale.locale();
    }
}
