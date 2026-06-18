import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResponse } from '../../core/models/api-response.model';

// Adaptador de entrada (asistente): alta de cuenta propia. El registro lo resuelve
// AuthService contra la API (que a su vez aprovisiona el usuario en Keycloak); aquí solo se
// valida el formulario y se muestra el primer error de negocio del envelope ApiResponse.
@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-stage">
      <div class="auth-card card">
        <div class="auth-head">
          <span class="brand-mark" aria-hidden="true"></span>
          <h1>Crea tu cuenta</h1>
          <p class="muted">Un paso para empezar a reservar.</p>
        </div>

        @if (error()) {
          <div class="alert alert-error">{{ error() }}</div>
        }

        <form class="stack" [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label for="fullName">Nombre completo</label>
            <input id="fullName" type="text" class="input" formControlName="fullName" autocomplete="name" />
          </div>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" type="email" class="input" formControlName="email" autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">Contraseña</label>
            <input id="password" type="password" class="input" formControlName="password" autocomplete="new-password" />
            <small class="muted">Mínimo 8 caracteres.</small>
          </div>
          <button type="submit" class="btn btn-accent btn-block" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Creando…' : 'Crear cuenta' }}
          </button>
        </form>

        <p class="text-center muted switch">
          ¿Ya tienes cuenta? <a routerLink="/login">Entra</a>
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
        max-width: 440px;
        background: var(--bg);
      }
      .auth-head {
        text-align: center;
        margin-bottom: var(--space-5);
      }
      .auth-head h1 {
        margin: var(--space-3) 0 var(--space-2);
        font-size: 1.8rem;
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
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        const body = err.error as ApiResponse<unknown> | undefined;
        const detail = body?.error?.details?.[0];
        this.error.set(detail ?? body?.error?.message ?? 'No pudimos crear la cuenta.');
      },
    });
  }
}
