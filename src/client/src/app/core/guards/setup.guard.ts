import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { ApiService } from '../api/api.service';

export const setupCompleteGuard: CanActivateFn = () =>
{
    const api = inject(ApiService);
    const router = inject(Router);

    return api.setupStatus().pipe(
        map(s => (s.complete ? true : router.createUrlTree(['/setup']))),
        catchError(() => of(router.createUrlTree(['/setup']))),
    );
};

export const setupIncompleteGuard: CanActivateFn = () =>
{
    const api = inject(ApiService);
    const router = inject(Router);

    return api.setupStatus().pipe(
        map(s => (s.complete ? router.createUrlTree(['/']) : true)),
        catchError(() => of(true)),
    );
};
