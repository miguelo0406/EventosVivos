import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// Adjunta el JWT a cada petición a la API y, ante un 401, intenta renovar el token una
// vez y reintentar; si el refresh falla, cierra sesión. Las llamadas a /auth/* no
// llevan token ni disparan refresh (son la puerta de entrada).
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isAuthCall =
    req.url.includes('/auth/login') ||
    req.url.includes('/auth/register') ||
    req.url.includes('/auth/refresh');

  const token = auth.accessToken;
  const authReq =
    !isAuthCall && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthCall && auth.accessToken) {
        return auth.refresh().pipe(
          switchMap((ok) => {
            if (!ok) {
              auth.logout('idle');
              return throwError(() => error);
            }
            const retried = req.clone({
              setHeaders: { Authorization: `Bearer ${auth.accessToken}` },
            });
            return next(retried);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
