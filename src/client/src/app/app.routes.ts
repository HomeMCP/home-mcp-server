import { Routes } from '@angular/router';

import { setupCompleteGuard, setupIncompleteGuard } from './core/guards/setup.guard';

export const routes: Routes = [
    {
        path: 'setup',
        loadComponent: () =>
            import('./features/setup/setup.component').then(m => m.SetupComponent),
        canActivate: [setupIncompleteGuard],
    },
    {
        path: '',
        loadComponent: () =>
            import('./layout/shell/shell.component').then(m => m.ShellComponent),
        canActivate: [setupCompleteGuard],
        children: [
            {
                path: '',
                pathMatch: 'full',
                redirectTo: 'dashboard',
            },
            {
                path: 'dashboard',
                loadComponent: () =>
                    import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
            },
            {
                path: 'plugins',
                loadComponent: () =>
                    import('./features/plugins/plugin-list/plugin-list.component').then(m => m.PluginListComponent),
            },
            {
                path: 'plugins/:id',
                loadComponent: () =>
                    import('./features/plugins/plugin-detail/plugin-detail.component').then(m => m.PluginDetailComponent),
            },
            {
                path: 'logs',
                loadComponent: () =>
                    import('./features/logs/logs.component').then(m => m.LogsComponent),
            },
        ],
    },
    { path: '**', redirectTo: '' },
];
