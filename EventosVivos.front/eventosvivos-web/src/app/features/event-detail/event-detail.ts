import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe, TitleCasePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { EventsService } from '../../core/services/events.service';
import { ReservationsService } from '../../core/services/reservations.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiResponse } from '../../core/models/api-response.model';
import { EventResponse, ReservationResponse } from '../../core/models/catalog.model';

// Detalle de evento + flujo de reserva (RF-03..RF-05). Cada regla de negocio que
// bloquea (antelación, límite de entradas, cupo) se muestra en el lugar de la acción.
@Component({
  selector: 'app-event-detail',
  imports: [DatePipe, CurrencyPipe, TitleCasePipe, ReactiveFormsModule, RouterLink],
  template: `
    <a routerLink="/" class="back muted">← Volver al catálogo</a>

    @if (loading()) {
      <p class="muted">Cargando…</p>
    } @else if (!event()) {
      <div class="alert alert-error">No encontramos este evento.</div>
    } @else {
      <article class="detail">
        <header class="detail-head">
          <div class="head-top">
            <span class="event-type">{{ event()!.type | titlecase }}</span>
            <span class="badge" [class]="'badge-' + event()!.status">{{ event()!.status | titlecase }}</span>
          </div>
          <h1>{{ event()!.title }}</h1>
          <p class="detail-meta">
            {{ event()!.venueName }} · {{ event()!.startDateTime | date: 'EEEE d MMMM, HH:mm' }} —
            {{ event()!.endDateTime | date: 'HH:mm' }}
          </p>
        </header>

        <div class="detail-grid">
          <div class="card">
            <h3>Sobre el evento</h3>
            <p>{{ event()!.description }}</p>
            <dl class="facts">
              <div><dt>Precio</dt><dd>{{ event()!.ticketPrice | currency: 'USD':'symbol':'1.0-0' }}</dd></div>
              <div><dt>Aforo</dt><dd>{{ event()!.maxCapacity }}</dd></div>
            </dl>
          </div>

          <aside class="card reserve">
            @if (reservation(); as r) {
              <h3>Tu reserva</h3>
              <p class="muted">{{ r.quantity }} entrada(s) · {{ r.buyerEmail }}</p>
              <p>
                Estado:
                <span class="badge" [class]="'badge-' + r.status">{{ statusLabel(r.status) }}</span>
              </p>
              @if (r.confirmationCode) {
                <p class="code">Código: <strong>{{ r.confirmationCode }}</strong></p>
              }
              @if (actionError()) {
                <div class="alert alert-error">{{ actionError() }}</div>
              }
              <div class="reserve-actions">
                @if (r.status === 'pendiente_pago') {
                  <button class="btn btn-accent" [disabled]="acting()" (click)="confirm(r.id)">Confirmar pago</button>
                }
                @if (r.status === 'pendiente_pago' || r.status === 'confirmada') {
                  <button class="btn btn-ghost" [disabled]="acting()" (click)="cancel(r.id)">Cancelar</button>
                }
              </div>
            } @else {
              <h3>Reservar entradas</h3>
              @if (reserveError()) {
                <div class="alert alert-error">{{ reserveError() }}</div>
              }
              <form class="stack" [formGroup]="form" (ngSubmit)="reserve()">
                <div class="field">
                  <label for="qty">Cantidad</label>
                  <input id="qty" class="input" type="number" min="1" formControlName="quantity" />
                </div>
                <div class="field">
                  <label for="name">Nombre del comprador</label>
                  <input id="name" class="input" type="text" formControlName="buyerName" />
                </div>
                <div class="field">
                  <label for="email">Email</label>
                  <input id="email" class="input" type="email" formControlName="buyerEmail" />
                </div>
                <button
                  type="submit"
                  class="btn btn-accent btn-block"
                  [disabled]="form.invalid || acting() || event()!.status !== 'activo'"
                >
                  {{ acting() ? 'Reservando…' : 'Reservar' }}
                </button>
                @if (event()!.status !== 'activo') {
                  <p class="muted">Solo se puede reservar en eventos activos.</p>
                }
              </form>
            }
          </aside>
        </div>
      </article>
    }
  `,
  styles: [
    `
      .back {
        display: inline-block;
        margin-bottom: var(--space-4);
        text-decoration: none;
      }
      .detail-head {
        margin-bottom: var(--space-6);
      }
      .head-top {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        margin-bottom: var(--space-2);
      }
      .event-type {
        font-size: 0.8rem;
        font-weight: 600;
        color: var(--brand);
      }
      .detail-meta {
        color: var(--ink-muted);
      }
      .detail-grid {
        display: grid;
        grid-template-columns: 1.6fr 1fr;
        gap: var(--space-5);
        align-items: start;
      }
      .facts {
        display: flex;
        gap: var(--space-6);
        margin: var(--space-5) 0 0;
      }
      .facts dt {
        font-size: 0.8rem;
        color: var(--ink-muted);
      }
      .facts dd {
        margin: 0;
        font-weight: 700;
        font-size: 1.1rem;
      }
      .reserve-actions {
        display: flex;
        gap: var(--space-3);
        margin-top: var(--space-4);
      }
      .code {
        font-size: 1.05rem;
      }
      @media (max-width: 760px) {
        .detail-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class EventDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly eventsService = inject(EventsService);
  private readonly reservationsService = inject(ReservationsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  protected readonly event = signal<EventResponse | null>(null);
  protected readonly reservation = signal<ReservationResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly reserveError = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    quantity: [1, [Validators.required, Validators.min(1)]],
    buyerName: ['', [Validators.required]],
    buyerEmail: [this.auth.email() ?? '', [Validators.required, Validators.email]],
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.eventsService.getById(id).subscribe({
      next: (event) => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  reserve(): void {
    if (this.form.invalid || !this.event()) {
      return;
    }
    this.acting.set(true);
    this.reserveError.set(null);

    this.reservationsService
      .reserve({ eventId: this.event()!.id, ...this.form.getRawValue() })
      .subscribe({
        next: (created) => {
          this.reservation.set(created);
          this.acting.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.acting.set(false);
          this.reserveError.set(this.messageOf(err) ?? 'No pudimos crear la reserva.');
        },
      });
  }

  confirm(id: string): void {
    this.runAction(this.reservationsService.confirmPayment(id));
  }

  cancel(id: string): void {
    this.runAction(this.reservationsService.cancel(id));
  }

  private runAction(action: ReturnType<ReservationsService['cancel']>): void {
    this.acting.set(true);
    this.actionError.set(null);
    action.subscribe({
      next: (updated) => {
        this.reservation.set(updated);
        this.acting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.acting.set(false);
        this.actionError.set(this.messageOf(err) ?? 'No pudimos completar la acción.');
      },
    });
  }

  protected statusLabel(status: string): string {
    return status.replace('_', ' ');
  }

  private messageOf(err: HttpErrorResponse): string | null {
    const body = err.error as ApiResponse<unknown> | undefined;
    return body?.error?.details?.[0] ?? body?.error?.message ?? null;
  }
}
