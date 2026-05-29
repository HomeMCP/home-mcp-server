import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLocale = 'en' | 'ru';

@Injectable({ providedIn: 'root' })
export class LocaleService
{
    private readonly platformId = inject(PLATFORM_ID);
    private readonly translate = inject(TranslateService);
    private readonly storageKey = 'hmcp-locale';

    readonly locale = signal<AppLocale>(this.loadLocale());

    constructor()
    {
        this.translate.use(this.locale());
    }

    setLocale(lang: AppLocale): void
    {
        this.locale.set(lang);
        this.translate.use(lang);
        if (isPlatformBrowser(this.platformId))
        {
            localStorage.setItem(this.storageKey, lang);
        }
    }

    /** Resolve a localised string from a label dict (e.g. plugin config schema labels). */
    resolve(labels: Record<string, string>): string
    {
        const lang = this.locale();
        return labels[lang] ?? labels['en'] ?? Object.values(labels)[0] ?? '';
    }

    private loadLocale(): AppLocale
    {
        if (!isPlatformBrowser(this.platformId))
        {
            return 'en';
        }
        const stored = localStorage.getItem(this.storageKey) as AppLocale | null;
        if (stored)
        {
            return stored;
        }
        return navigator.language.startsWith('ru') ? 'ru' : 'en';
    }
}
