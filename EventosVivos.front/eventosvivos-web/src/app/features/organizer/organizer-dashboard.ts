import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe, TitleCasePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EventsService } from '../../core/services/events.service';
import { EventResponse, EventStatus } from '../../core/models/catalog.model';

// Panel del organizador: vista densa (tabla) de todos los eventos para gestionarlos.
// Se diferencia del catálogo público (tarjetas, orientado a reservar) porque aquí la
// tarea es operar: crear, revisar ocupación y administrar reservas.
@Component({
  selector: 'app-organizer-dashboard',
  imports: [DatePipe, CurrencyPipe, TitleCasePipe, FormsModule, RouterLink],
  template: `
    <section class="head">
      <div>
        <h1>Panel de organizador</h1>
        <p class="muted">Crea eventos y administra ocupación, pagos y reservas.</p>
      </div>
      <div class="head-actions">
        <a routerLink="/admin/venues" class="btn btn-ghost">Lugares</a>
        <a routerLink="/admin/events/new" class="btn btn-accent">Crear evento</a>
      </div>
    </section>

    <div class="toolbar card">
      <div class="field">
        <label for="f-status">Estado</label>
        <select id="f-status" class="input" [(ngModel)]="status" (change)="reload()">
          <option [ngValue]="undefined">Todos</option>
          <option value="activo">Activo</option>
          <option value="completado">Completado</option>
          <option value="cancelado">Cancelado</option>
        </select>
      </div>
      <span class="count muted">{{ events().length }} evento(s)</span>
    </div>

    @if (loading()) {
      <p class="muted">Cargando eventos…</p>
    } @else if (error()) {
      <div class="alert alert-error">{{ error() }}</div>
    } @else if (events().length === 0) {
      <div class="empty card text-center">
        <h3>Aún no hay eventos</h3>
        <p class="muted">Crea el primero para empezar a recibir reservas.</p>
        <a routerLink="/admin/events/new" class="btn btn-accent">Crear evento</a>
      </div>
    } @else {
      <div class="card table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>Evento</th>
              <th>Tipo</th>
              <th>Lugar</th>
              <th>Inicio</th>
              <th class="num">Precio</th>
              <th>Estado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (event of events(); track event.id) {
              <tr>
                <td class="title">{{ event.title }}</td>
                <td>{{ event.type | titlecase }}</td>
                <td>{{ event.venueName }}</td>
                <td>{{ event.startDateTime | date: 'd MMM y, HH:mm' }}</td>
                <td class="num">{{ event.ticketPrice | currency: 'USD':'symbol':'1.0-0' }}</td>
                <td><span class="badge" [class]="'badge-' + event.status">{{ event.status | titlecase }}</span></td>
                <td class="num">
                  <a class="manage" [routerLink]="['/admin/events', event.id]">Gestionar →</a>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: [
    `
      .head {
        display: flex;
        align-items: flex-end;
        justify-content: space-between;
        gap: var(--space-4);
        margin-bottom: var(--space-5);
      }
      .head-actions {
        display: flex;
        gap: var(--space-3);
      }
      .head .btn {
        text-decoration: none;
        white-space: nowrap;
      }
      .toolbar {
        display: flex;
        align-items: flex-end;
        gap: var(--space-5);
        margin-bottom: var(--space-5);
        padding: var(--space-4) var(--space-5);
      }
      .count {
        font-size: 0.85rem;
        margin-bottom: 0.55rem;
      }
      .table-wrap {
        padding: 0;
        overflow-x: auto;
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
      .table tbody tr:hover {
        background: var(--surface-2);
      }
      .title {
        font-weight: 600;
      }
      .num {
        text-align: right;
      }
      .manage {
        text-decoration: none;
        font-weight: 600;
        color: var(--brand);
      }
      .empty {
        padding: var(--space-8) var(--space-5);
      }
      .empty .btn {
        margin-top: var(--space-3);
        text-decoration: none;
      }
    `,
  ],
})
export class OrganizerDashboardComponent {
  private readonly eventsService = inject(EventsService);

  protected readonly events = signal<EventResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected status: EventStatus | undefined;

  constructor() {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.eventsService.list({ status: this.status }).subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los eventos.');
        this.loading.set(false);
      },
    });
  }
}
