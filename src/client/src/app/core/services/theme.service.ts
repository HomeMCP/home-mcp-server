import { isPlatformBrowser } from '@angular/common';
import { effect, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService
{
    private readonly platformId = inject(PLATFORM_ID);
    private readonly storageKey = 'hmcp-theme';

    readonly theme = signal<Theme>(this.loadTheme());

    constructor()
    {
        effect(() =>
        {
            const t = this.theme();
            if (isPlatformBrowser(this.platformId))
            {
                document.documentElement.setAttribute('data-theme', t);
                localStorage.setItem(this.storageKey, t);
            }
        });
    }

    toggle(): void
    {
        this.theme.update(t => (t === 'light' ? 'dark' : 'light'));
    }

    private loadTheme(): Theme
    {
        if (!isPlatformBrowser(this.platformId))
        {
            return 'light';
        }
        const stored = localStorage.getItem(this.storageKey) as Theme | null;
        if (stored)
        {
            return stored;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
}
