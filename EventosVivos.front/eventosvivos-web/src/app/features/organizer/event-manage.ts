import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe, DecimalPipe, TitleCasePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { EventsService } from '../../core/services/events.service';
import { ReservationsService } from '../../core/services/reservations.service';
import { ApiResponse } from '../../core/models/api-response.model';
import {
  EventResponse,
  OccupancyReportResponse,
  ReservationResponse,
} from '../../core/models/catalog.model';

// Gestión de un evento (organizador): reporte de ocupación (RF-06) y administración de
// reservas (RF-04 confirmar pago, RF-05 cancelar). Tras cada acción se re-consulta el
// reporte porque cancelar una reserva puede liberar cupo e ingresos (una 'cancelada' sí
// libera; una 'perdida' no), de modo que los totales siempre reflejen el estado real.
@Component({
  selector: 'app-event-manage',
  imports: [DatePipe, CurrencyPipe, DecimalPipe, TitleCasePipe, RouterLink],
  template: `
    <a routerLink="/admin" class="back muted">← Volver al panel</a>

    @if (loading()) {
      <p class="muted">Cargando…</p>
    } @else if (!event()) {
      <div class="alert alert-error">No encontramos este evento.</div>
    } @else {
      <header class="head">
        <div class="head-main">
          <div class="head-top">
            <span class="type">{{ event()!.type | titlecase }}</span>
            <span class="badge" [class]="'badge-' + event()!.status">{{ event()!.status | titlecase }}</span>
          </div>
          <h1>{{ event()!.title }}</h1>
          <p class="muted">
            {{ event()!.venueName }} · {{ event()!.startDateTime | date: 'EEEE d MMMM, HH:mm' }} —
            {{ event()!.endDateTime | date: 'HH:mm' }}
          </p>
        </div>

        @if (event()!.status === 'activo') {
          <div class="head-action">
            @if (confirmingCancel()) {
              <span class="confirm-text muted">¿Cancelar el evento?</span>
              <button class="btn btn-ghost btn-sm" [disabled]="cancellingEvent()" (click)="confirmingCancel.set(false)">
                No
              </button>
              <button class="btn btn-danger btn-sm" [disabled]="cancellingEvent()" (click)="cancelEvent()">
                {{ cancellingEvent() ? 'Cancelando…' : 'Sí, cancelar' }}
              </button>
            } @else {
              <button class="btn btn-ghost btn-sm" (click)="confirmingCancel.set(true)">Cancelar evento</button>
            }
          </div>
        }
      </header>

      @if (eventError()) {
        <div class="alert alert-error">{{ eventError() }}</div>
      }

      @if (report(); as r) {
        <section class="card report">
          <div class="report-head">
            <h3>Ocupación</h3>
            <span class="pct">{{ r.occupancyPercentage | number: '1.0-1' }}%</span>
          </div>
          <div class="bar" role="img" [attr.aria-label]="'Ocupación ' + (r.occupancyPercentage | number: '1.0-1') + '%'">
            <span class="bar-fill" [style.width.%]="clampPercent(r.occupancyPercentage)"></span>
          </div>
          <dl class="stats">
            <div><dt>Vendidas</dt><dd>{{ r.totalSoldTickets }}</dd></div>
            <div><dt>Disponibles</dt><dd>{{ r.totalAvailableTickets }}</dd></div>
            <div><dt>Aforo</dt><dd>{{ r.totalSoldTickets + r.totalAvailableTickets }}</dd></div>
            <div><dt>Ingresos</dt><dd>{{ r.totalRevenue | currency: 'USD':'symbol':'1.0-2' }}</dd></div>
          </dl>
        </section>
      }

      <section class="card reservations">
        <h3>Reservas</h3>
        @if (actionError()) {
          <div class="alert alert-error">{{ actionError() }}</div>
        }
        @if (reservations().length === 0) {
          <p class="muted">Este evento aún no tiene reservas.</p>
        } @else {
          <div class="table-wrap">
            <table class="table">
              <thead>
                <tr>
                  <th>Comprador</th>
                  <th class="num">Cantidad</th>
                  <th>Estado</th>
                  <th>Creada</th>
                  <th>Código</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                @for (reservation of reservations(); track reservation.id) {
                  <tr>
                    <td>
                      <span class="buyer">{{ reservation.buyerName }}</span>
                      <span class="buyer-email muted">{{ reservation.buyerEmail }}</span>
                    </td>
                    <td class="num">{{ reservation.quantity }}</td>
                    <td>
                      <span class="badge" [class]="'badge-' + reservation.status">
                        {{ statusLabel(reservation.status) | titlecase }}
                      </span>
                    </td>
                    <td>{{ reservation.createdAt | date: 'd MMM y, HH:mm' }}</td>
                    <td class="code">{{ reservation.confirmationCode ?? '—' }}</td>
                    <td class="num actions">
                      @if (reservation.status === 'pendiente_pago') {
                        <button class="btn btn-accent btn-sm" [disabled]="actingId() === reservation.id"
                                (click)="confirmPayment(reservation.id)">Confirmar pago</button>
                      }
                      @if (reservation.status === 'pendiente_pago' || reservation.status === 'confirmada') {
                        <button class="btn btn-ghost btn-sm" [disabled]="actingId() === reservation.id"
                                (click)="cancelReservation(reservation.id)">Cancelar</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
    }
  `,
  styles: [
    `
      .back {
        display: inline-block;
        margin-bottom: var(--space-4);
        text-decoration: none;
      }
      .head {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--space-4);
        margin-bottom: var(--space-5);
      }
      .head-top {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        margin-bottom: var(--space-2);
      }
      .type {
        font-size: 0.8rem;
        font-weight: 600;
        color: var(--brand);
      }
      .head-action {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        flex-shrink: 0;
      }
      .confirm-text {
        font-size: 0.85rem;
      }
      .report {
        margin-bottom: var(--space-5);
      }
      .report-head {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
      }
      .report-head h3 {
        margin: 0;
      }
      .pct {
        font-family: var(--font-display);
        font-size: 1.6rem;
        font-weight: 600;
        color: var(--brand);
      }
      .bar {
        height: 10px;
        border-radius: var(--radius-pill);
        background: var(--surface-2);
        overflow: hidden;
        margin: var(--space-3) 0 var(--space-5);
      }
      .bar-fill {
        display: block;
        height: 100%;
        border-radius: var(--radius-pill);
        background: var(--accent);
        transition: width 0.35s ease;
      }
      .stats {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: var(--space-4);
        margin: 0;
      }
      .stats dt {
        font-size: 0.8rem;
        color: var(--ink-muted);
      }
      .stats dd {
        margin: 0.2rem 0 0;
        font-weight: 700;
        font-size: 1.25rem;
      }
      .table-wrap {
        overflow-x: auto;
        margin-top: var(--space-3);
      }
      .table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.92rem;
      }
      .table th,
      .table td {
        text-align: left;
        padding: var(--space-3) var(--space-4);
        border-bottom: 1px solid var(--border);
        white-space: nowrap;
        vertical-align: middle;
      }
      .table th {
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--ink-muted);
        font-weight: 600;
      }
      .table tbody tr:last-child td {
        border-bottom: none;
      }
      .num {
        text-align: right;
      }
      .buyer {
        display: block;
        font-weight: 600;
      }
      .buyer-email {
        display: block;
        font-size: 0.82rem;
      }
      .code {
        font-variant-numeric: tabular-nums;
        letter-spacing: 0.02em;
      }
      .actions {
        display: flex;
        gap: var(--space-2);
        justify-content: flex-end;
      }
      .btn-sm {
        padding: 0.4rem 0.85rem;
        font-size: 0.82rem;
      }
      .btn-danger {
        background: var(--danger);
        color: var(--on-brand);
      }
      .btn-danger:not(:disabled):hover {
        background: var(--danger);
        filter: brightness(0.94);
      }
      @media (max-width: 640px) {
        .stats {
          grid-template-columns: 1fr 1fr;
        }
        .head {
          flex-direction: column;
        }
      }
    `,
  ],
})
export class EventManageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly eventsService = inject(EventsService);
  private readonly reservationsService = inject(ReservationsService);

  protected readonly event = signal<EventResponse | null>(null);
  protected readonly report = signal<OccupancyReportResponse | null>(null);
  protected readonly reservations = signal<ReservationResponse[]>([]);
  protected readonly loading = signal(true);

  // Una sola reserva en acción a la vez: deshabilita sus botones mientras la API responde.
  protected readonly actingId = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);

  protected readonly confirmingCancel = signal(false);
  protected readonly cancellingEvent = signal(false);
  protected readonly eventError = signal<string | null>(null);

  private readonly eventId = this.route.snapshot.paramMap.get('id')!;

  constructor() {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    // forkJoin: las tres consultas son independientes, se resuelven en paralelo.
    forkJoin({
      event: this.eventsService.getById(this.eventId),
      report: this.eventsService.occupancyReport(this.eventId),
      reservations: this.eventsService.reservations(this.eventId),
    }).subscribe({
      next: ({ event, report, reservations }) => {
        this.event.set(event);
        this.report.set(report);
        this.reservations.set(reservations);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  confirmPayment(reservationId: string): void {
    this.runReservationAction(reservationId, this.reservationsService.confirmPayment(reservationId));
  }

  cancelReservation(reservationId: string): void {
    this.runReservationAction(reservationId, this.reservationsService.cancel(reservationId));
  }

  private runReservationAction(
    reservationId: string,
    action: ReturnType<ReservationsService['cancel']>,
  ): void {
    this.actingId.set(reservationId);
    this.actionError.set(null);
    action.subscribe({
      next: (updated) => {
        this.reservations.update((list) =>
          list.map((reservation) => (reservation.id === updated.id ? updated : reservation)),
        );
        this.actingId.set(null);
        this.refreshReport();
      },
      error: (err: HttpErrorResponse) => {
        this.actingId.set(null);
        this.actionError.set(this.messageOf(err) ?? 'No pudimos completar la acción.');
      },
    });
  }

  cancelEvent(): void {
    this.cancellingEvent.set(true);
    this.eventError.set(null);
    this.eventsService.cancel(this.eventId).subscribe({
      next: () => {
        this.confirmingCancel.set(false);
        this.cancellingEvent.set(false);
        this.eventsService.getById(this.eventId).subscribe((event) => this.event.set(event));
        this.refreshReport();
      },
      error: (err: HttpErrorResponse) => {
        this.cancellingEvent.set(false);
        this.eventError.set(this.messageOf(err) ?? 'No pudimos cancelar el evento.');
      },
    });
  }

  private refreshReport(): void {
    this.eventsService.occupancyReport(this.eventId).subscribe((report) => this.report.set(report));
  }

  protected clampPercent(value: number): number {
    return Math.max(0, Math.min(100, value));
  }

  protected statusLabel(status: string): string {
    return status.replace('_', ' ');
  }

  private messageOf(err: HttpErrorResponse): string | null {
    const body = err.error as ApiResponse<unknown> | undefined;
    return body?.error?.details?.[0] ?? body?.error?.message ?? null;
  }
}
