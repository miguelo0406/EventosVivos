import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

// Protege las rutas privadas: sin sesión válida, redirige a /login.
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated() ? true : router.createUrlTree(['/login']);
};

// Protege la superficie de organizador (/admin/**): exige el rol 'organizer'. Sin sesión va
// a /login; autenticado pero sin el rol, al catálogo. Es defensa en profundidad junto a la
// policy 'Organizer' del backend (el guard solo mejora la UX; la barrera real es la API).
export const organizerGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  return auth.isOrganizer() ? true : router.createUrlTree(['/']);
};
