import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

// Shell de las superficies autenticadas: cabecera (penumbra índigo, "antes de la
// función") + contenido. La calidez vive en marca y tipografía, no en el fondo.
@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <header class="topbar">
      <div class="container topbar-inner">
        <a routerLink="/" class="brand">
          <span class="brand-mark" aria-hidden="true"></span>
          <span class="brand-name">EventosVivos</span>
        </a>
        <nav class="nav">
          <a routerLink="/" routerLinkActive="is-active" [routerLinkActiveOptions]="{ exact: true }" class="nav-link">Catálogo</a>
          @if (auth.isOrganizer()) {
            <a routerLink="/admin" routerLinkActive="is-active" class="nav-link">Organizar</a>
          }
        </nav>
        <div class="account">
          <span class="account-email">{{ auth.email() }}</span>
          <button type="button" class="btn btn-ghost btn-sm" (click)="auth.logout()">Salir</button>
        </div>
      </div>
    </header>

    <main class="container main">
      <router-outlet />
    </main>
  `,
  styles: [
    `
      .topbar {
        background: var(--brand-strong);
        color: var(--on-brand);
      }
      .topbar-inner {
        display: flex;
        align-items: center;
        gap: var(--space-5);
        height: 64px;
      }
      .brand {
        display: inline-flex;
        align-items: center;
        gap: 0.6rem;
        color: var(--on-brand);
        text-decoration: none;
      }
      .brand-mark {
        width: 12px;
        height: 12px;
        border-radius: 50%;
        background: var(--accent);
        box-shadow: 0 0 14px 2px var(--accent);
      }
      .brand-name {
        font-family: var(--font-display);
        font-size: 1.25rem;
        font-weight: 600;
        letter-spacing: -0.01em;
      }
      .nav {
        flex: 1;
        display: flex;
        gap: var(--space-4);
      }
      .nav-link {
        color: var(--on-brand);
        opacity: 0.85;
        text-decoration: none;
        font-weight: 500;
        font-size: 0.95rem;
      }
      .nav-link:hover {
        opacity: 1;
      }
      .nav-link.is-active {
        opacity: 1;
        border-bottom: 2px solid var(--accent);
        padding-bottom: 2px;
      }
      .account {
        display: flex;
        align-items: center;
        gap: var(--space-3);
      }
      .account-email {
        font-size: 0.85rem;
        opacity: 0.85;
      }
      .btn-sm {
        padding: 0.4rem 0.85rem;
        font-size: 0.85rem;
      }
      .topbar .btn-ghost {
        color: var(--on-brand);
        border-color: oklch(1 0 0 / 0.3);
      }
      .topbar .btn-ghost:hover {
        background: oklch(1 0 0 / 0.12);
      }
      .main {
        padding-top: var(--space-6);
        padding-bottom: var(--space-8);
      }
      @media (max-width: 560px) {
        .account-email {
          display: none;
        }
      }
    `,
  ],
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);
}
