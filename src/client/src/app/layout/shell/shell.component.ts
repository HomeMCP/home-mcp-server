import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AppLocale, LocaleService } from '../../core/services/locale.service';
import { ThemeService } from '../../core/services/theme.service';

interface NavItem
{
    path: string;
    icon: string;
    labelKey: string;
}

@Component({
    selector: 'app-shell',
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        RouterOutlet,
        RouterLink,
        RouterLinkActive,
        MatSidenavModule,
        MatToolbarModule,
        MatListModule,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule,
        TranslateModule,
    ],
    templateUrl: './shell.component.html',
    styleUrl: './shell.component.scss',
})
export class ShellComponent
{
    readonly theme = inject(ThemeService);
    readonly locale = inject(LocaleService);

    readonly sidenavOpen = signal(true);

    readonly themeIcon = computed(() =>
        this.theme.theme() === 'dark' ? 'light_mode' : 'dark_mode',
    );

    readonly navItems: NavItem[] = [
        { path: 'dashboard', icon: 'dashboard', labelKey: 'nav.dashboard' },
        { path: 'plugins',   icon: 'extension', labelKey: 'nav.plugins'   },
        { path: 'logs',      icon: 'list_alt',  labelKey: 'nav.logs'      },
    ];

    toggleSidenav(): void
    {
        this.sidenavOpen.update(v => !v);
    }

    toggleLocale(): void
    {
        const next: AppLocale = this.locale.locale() === 'en' ? 'ru' : 'en';
        this.locale.setLocale(next);
    }
}
