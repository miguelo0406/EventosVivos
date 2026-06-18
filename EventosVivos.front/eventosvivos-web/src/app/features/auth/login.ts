import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResponse } from '../../core/models/api-response.model';

// Adaptador de entrada (asistente): pantalla de inicio de sesión, estética "antes de la
// función" (penumbra índigo + marquesina ámbar). El componente solo orquesta el formulario
// y el feedback; toda la lógica de sesión (tokens, refresh, idle) vive en AuthService (SRP).
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-stage">
      <div class="auth-card card">
        <div class="auth-head">
          <span class="brand-mark" aria-hidden="true"></span>
          <h1>Entra a la sala</h1>
          <p class="muted">Reserva conferencias, talleres y conciertos en vivo.</p>
        </div>

        @if (idle()) {
          <div class="alert alert-error">Tu sesión se cerró por inactividad. Vuelve a entrar.</div>
        }
        @if (error()) {
          <div class="alert alert-error">{{ error() }}</div>
        }

        <form class="stack" [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label for="email">Email</label>
            <input id="email" type="email" class="input" formControlName="email" autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">Contraseña</label>
            <input id="password" type="password" class="input" formControlName="password" autocomplete="current-password" />
          </div>
          <button type="submit" class="btn btn-accent btn-block" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Entrando…' : 'Entrar' }}
          </button>
        </form>

        <p class="text-center muted switch">
          ¿No tienes cuenta? <a routerLink="/register">Crea una</a>
        </p>
      </div>
    </div>
  `,
  styles: [
    `
      .auth-stage {
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: var(--space-5);
        background:
          radial-gradient(1200px 600px at 50% -10%, var(--brand-soft), transparent 60%),
          var(--brand-strong);
      }
      .auth-card {
        width: 100%;
        max-width: 420px;
        background: var(--bg);
      }
      .auth-head {
        text-align: center;
        margin-bottom: var(--space-5);
      }
      .auth-head h1 {
        margin: var(--space-3) 0 var(--space-2);
        font-size: 1.9rem;
      }
      .brand-mark {
        display: inline-block;
        width: 14px;
        height: 14px;
        border-radius: 50%;
        background: var(--accent);
        box-shadow: 0 0 18px 3px var(--accent);
      }
      .switch {
        margin-top: var(--space-5);
        font-size: 0.9rem;
      }
    `,
  ],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly idle = signal(this.route.snapshot.queryParamMap.get('idle') === '1');

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        const body = err.error as ApiResponse<unknown> | undefined;
        this.error.set(body?.error?.message ?? 'No pudimos iniciar sesión. Revisa tus credenciales.');
      },
    });
  }
}
