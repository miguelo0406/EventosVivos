import { Routes } from '@angular/router';
import { authGuard, organizerGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register').then((m) => m.RegisterComponent),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/catalog/catalog').then((m) => m.CatalogComponent),
      },
      {
        path: 'events/:id',
        loadComponent: () =>
          import('./features/event-detail/event-detail').then((m) => m.EventDetailComponent),
      },
      {
        path: 'admin',
        canActivate: [organizerGuard],
        loadComponent: () =>
          import('./features/organizer/organizer-dashboard').then((m) => m.OrganizerDashboardComponent),
      },
      {
        // La ruta literal va antes que la paramétrica para que 'new' no se interprete como :id.
        path: 'admin/events/new',
        canActivate: [organizerGuard],
        loadComponent: () =>
          import('./features/organizer/event-create').then((m) => m.EventCreateComponent),
      },
      {
        path: 'admin/venues',
        canActivate: [organizerGuard],
        loadComponent: () =>
          import('./features/organizer/venues-admin').then((m) => m.VenuesAdminComponent),
      },
      {
        path: 'admin/events/:id',
        canActivate: [organizerGuard],
        loadComponent: () =>
          import('./features/organizer/event-manage').then((m) => m.EventManageComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
