import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, shareReplay, tap, map, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';

// Control de sesión en el cliente. Mantiene los tokens, renueva el access token (de 5
// min) mientras el usuario está activo y cierra la sesión tras 5 min de inactividad.
// El idle también se refuerza en el servidor (SSO Session Idle de Keycloak): si no hay
// refresh en 5 min, el refresh token muere y el siguiente intento falla → logout.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private static readonly ACCESS_KEY = 'ev_access';
  private static readonly REFRESH_KEY = 'ev_refresh';
  private static readonly EMAIL_KEY = 'ev_email';
  private static readonly ROLES_KEY = 'ev_roles';
  private static readonly IDLE_MS = 5 * 60 * 1000;
  private static readonly REFRESH_MARGIN_MS = 30 * 1000;

  private readonly _email = signal<string | null>(localStorage.getItem(AuthService.EMAIL_KEY));
  readonly email = this._email.asReadonly();
  readonly isAuthenticated = computed(() => this._email() !== null && this.accessToken !== null);

  private readonly _roles = signal<string[]>(this.readStoredRoles());
  readonly roles = this._roles.asReadonly();
  // Decide si el usuario puede entrar a la superficie de organizador (RF-01, RF-06). Es solo
  // una pista de UI: la autorización real la impone el backend con la policy 'Organizer'.
  readonly isOrganizer = computed(() => this._roles().includes('organizer'));

  private refreshTimer?: ReturnType<typeof setTimeout>;
  private lastActivity = Date.now();
  private refreshInFlight$?: Observable<boolean>;

  constructor() {
    // Si arrancamos con sesión guardada, retomamos el ciclo de renovación.
    if (this.accessToken && this._email()) {
      this.trackActivity();
      this.scheduleRefresh();
    }
  }

  get accessToken(): string | null {
    return localStorage.getItem(AuthService.ACCESS_KEY);
  }

  private get refreshToken(): string | null {
    return localStorage.getItem(AuthService.REFRESH_KEY);
  }

  login(request: LoginRequest): Observable<void> {
    return this.http
      .post<ApiResponse<AuthResponse>>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(map((response) => this.onAuthenticated(response.data!)));
  }

  register(request: RegisterRequest): Observable<void> {
    return this.http
      .post<ApiResponse<AuthResponse>>(`${environment.apiBaseUrl}/auth/register`, request)
      .pipe(map((response) => this.onAuthenticated(response.data!)));
  }

  // Renueva el access token. Comparte la llamada en vuelo para evitar refrescos
  // simultáneos cuando varias peticiones reciben 401 a la vez.
  refresh(): Observable<boolean> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    const token = this.refreshToken;
    if (!token) {
      return of(false);
    }

    this.refreshInFlight$ = this.http
      .post<ApiResponse<AuthResponse>>(`${environment.apiBaseUrl}/auth/refresh`, { refreshToken: token })
      .pipe(
        map((response) => {
          this.persist(response.data!);
          this.scheduleRefresh();
          return true;
        }),
        catchError(() => of(false)),
        tap(() => (this.refreshInFlight$ = undefined)),
        shareReplay(1),
      );

    return this.refreshInFlight$;
  }

  logout(reason?: 'idle'): void {
    this.clearTimers();
    localStorage.removeItem(AuthService.ACCESS_KEY);
    localStorage.removeItem(AuthService.REFRESH_KEY);
    localStorage.removeItem(AuthService.EMAIL_KEY);
    localStorage.removeItem(AuthService.ROLES_KEY);
    this._email.set(null);
    this._roles.set([]);
    this.router.navigate(['/login'], reason === 'idle' ? { queryParams: { idle: 1 } } : undefined);
  }

  private onAuthenticated(auth: AuthResponse): void {
    this.persist(auth);
    this.lastActivity = Date.now();
    this.trackActivity();
    this.scheduleRefresh();
  }

  private persist(auth: AuthResponse): void {
    localStorage.setItem(AuthService.ACCESS_KEY, auth.accessToken);
    localStorage.setItem(AuthService.REFRESH_KEY, auth.refreshToken);
    localStorage.setItem(AuthService.EMAIL_KEY, auth.email);
    this._email.set(auth.email);

    // Los roles no viajan en el AuthResponse: viven dentro del access token (realm_access).
    const roles = this.decodeRoles(auth.accessToken);
    localStorage.setItem(AuthService.ROLES_KEY, JSON.stringify(roles));
    this._roles.set(roles);
  }

  private readStoredRoles(): string[] {
    const raw = localStorage.getItem(AuthService.ROLES_KEY);
    if (!raw) {
      return [];
    }
    try {
      const parsed = JSON.parse(raw) as unknown;
      return Array.isArray(parsed) ? (parsed as string[]) : [];
    } catch {
      return [];
    }
  }

  // Lee los roles del realm del payload del access token. NO valida la firma (eso ya lo hizo
  // el backend al emitirlo): aquí solo se usa para decidir qué mostrar. Keycloak los anida en
  // realm_access.roles y el payload va en base64url (se normaliza a base64 antes de atob).
  private decodeRoles(accessToken: string): string[] {
    const payload = accessToken.split('.')[1];
    if (!payload) {
      return [];
    }
    try {
      const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      const claims = JSON.parse(atob(padded)) as { realm_access?: { roles?: string[] } };
      return claims.realm_access?.roles ?? [];
    } catch {
      return [];
    }
  }

  // Programa la renovación poco antes de que expire el access token.
  private scheduleRefresh(): void {
    this.clearTimers();
    const delay = Math.max(AuthService.IDLE_MS - AuthService.REFRESH_MARGIN_MS, 10_000);

    this.refreshTimer = setTimeout(() => {
      const idleFor = Date.now() - this.lastActivity;
      if (idleFor >= AuthService.IDLE_MS) {
        // 5 minutos sin actividad → cierre de sesión por inactividad.
        this.logout('idle');
        return;
      }

      this.refresh().subscribe((ok) => {
        if (!ok) {
          this.logout('idle');
        }
      });
    }, delay);
  }

  private clearTimers(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = undefined;
    }
  }

  private activityBound = false;
  private trackActivity(): void {
    if (this.activityBound) {
      return;
    }
    this.activityBound = true;
    const onActivity = () => (this.lastActivity = Date.now());
    ['click', 'keydown', 'mousemove', 'scroll', 'touchstart'].forEach((evt) =>
      window.addEventListener(evt, onActivity, { passive: true }),
    );
  }
}
