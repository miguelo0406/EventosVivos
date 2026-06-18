import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe, TitleCasePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EventsService } from '../../core/services/events.service';
import { VenuesService } from '../../core/services/venues.service';
import {
  EventResponse,
  EventStatus,
  EventType,
  VenueResponse,
} from '../../core/models/catalog.model';

// Catálogo público (asistentes): superficie "Committed" — color de marca presente,
// estados de reserva/evento inequívocos. Cada tarjeta lleva al detalle para reservar.
@Component({
  selector: 'app-catalog',
  imports: [DatePipe, CurrencyPipe, TitleCasePipe, FormsModule, RouterLink],
  template: `
    <section class="hero">
      <h1>Lo que está por empezar</h1>
      <p class="muted">Conferencias, talleres y conciertos. Reserva antes de que bajen las luces.</p>
    </section>

    <div class="filters card">
      <div class="field">
        <label for="f-title">Buscar por título</label>
        <input id="f-title" class="input" type="text" [(ngModel)]="title" (input)="reload()" placeholder="Ej. arquitectura" />
      </div>
      <div class="field">
        <label for="f-type">Tipo</label>
        <select id="f-type" class="input" [(ngModel)]="type" (change)="reload()">
          <option [ngValue]="undefined">Todos</option>
          <option value="conferencia">Conferencia</option>
          <option value="taller">Taller</option>
          <option value="concierto">Concierto</option>
        </select>
      </div>
      <div class="field">
        <label for="f-status">Estado</label>
        <select id="f-status" class="input" [(ngModel)]="status" (change)="reload()">
          <option [ngValue]="undefined">Todos</option>
          <option value="activo">Activo</option>
          <option value="completado">Completado</option>
          <option value="cancelado">Cancelado</option>
        </select>
      </div>
      <div class="field">
        <label for="f-venue">Lugar</label>
        <select id="f-venue" class="input" [(ngModel)]="venueId" (change)="reload()">
          <option [ngValue]="undefined">Todos</option>
          @for (venue of venues(); track venue.id) {
            <option [ngValue]="venue.id">{{ venue.name }}</option>
          }
        </select>
      </div>
      <div class="field">
        <label for="f-from">Desde (inicio)</label>
        <input id="f-from" class="input" type="date" [(ngModel)]="fromDate" (change)="reload()" />
      </div>
      <div class="field">
        <label for="f-to">Hasta (inicio)</label>
        <input id="f-to" class="input" type="date" [(ngModel)]="toDate" (change)="reload()" />
      </div>
    </div>

    @if (loading()) {
      <p class="muted">Cargando eventos…</p>
    } @else if (error()) {
      <div class="alert alert-error">{{ error() }}</div>
    } @else if (events().length === 0) {
      <div class="empty card text-center">
        <h3>No hay eventos con estos filtros</h3>
        <p class="muted">Prueba quitar algún filtro.</p>
      </div>
    } @else {
      <div class="grid">
        @for (event of events(); track event.id) {
          <a class="event-card card" [routerLink]="['/events', event.id]">
            <div class="event-top">
              <span class="event-type">{{ event.type | titlecase }}</span>
              <span class="badge" [class]="'badge-' + event.status">{{ event.status | titlecase }}</span>
            </div>
            <h3 class="event-title">{{ event.title }}</h3>
            <p class="event-meta">
              {{ event.venueName }} · {{ event.startDateTime | date: 'EEE d MMM, HH:mm' }}
            </p>
            <div class="event-foot">
              <span class="price">{{ event.ticketPrice | currency: 'USD':'symbol':'1.0-0' }}</span>
              <span class="cap muted">Aforo {{ event.maxCapacity }}</span>
            </div>
          </a>
        }
      </div>
    }
  `,
  styles: [
    `
      .hero {
        margin-bottom: var(--space-5);
      }
      .filters {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: var(--space-4);
        margin-bottom: var(--space-6);
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: var(--space-5);
      }
      .event-card {
        display: block;
        text-decoration: none;
        color: inherit;
        transition: transform 0.14s ease, box-shadow 0.2s ease, border-color 0.2s ease;
      }
      .event-card:hover {
        transform: translateY(-3px);
        box-shadow: var(--shadow-hover);
        border-color: var(--border-strong);
      }
      .event-top {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: var(--space-2);
        margin-bottom: var(--space-3);
      }
      .event-type {
        font-size: 0.78rem;
        font-weight: 600;
        color: var(--brand);
        letter-spacing: 0.02em;
      }
      .event-title {
        margin: 0 0 var(--space-2);
        font-size: 1.4rem;
      }
      .event-meta {
        color: var(--ink-muted);
        font-size: 0.9rem;
        margin: 0 0 var(--space-4);
      }
      .event-foot {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
      }
      .price {
        font-weight: 700;
        font-size: 1.1rem;
        color: var(--ink);
      }
      .cap {
        font-size: 0.85rem;
      }
      .empty {
        padding: var(--space-8) var(--space-5);
      }
      @media (max-width: 720px) {
        .filters {
          grid-template-columns: 1fr 1fr;
        }
      }
    `,
  ],
})
export class CatalogComponent {
  private readonly eventsService = inject(EventsService);
  private readonly venuesService = inject(VenuesService);

  protected readonly events = signal<EventResponse[]>([]);
  protected readonly venues = signal<VenueResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected title = '';
  protected type: EventType | undefined;
  protected status: EventStatus | undefined;
  protected venueId: number | undefined;
  protected fromDate = '';
  protected toDate = '';

  constructor() {
    this.venuesService.list().subscribe((venues) => this.venues.set(venues));
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.eventsService
      .list({
        title: this.title || undefined,
        type: this.type,
        status: this.status,
        venueId: this.venueId,
        // El input type="date" no lleva zona horaria: Date lo interpreta como el inicio del
        // día en hora LOCAL del usuario, y toISOString() lo convierte al UTC real equivalente.
        // "Hasta" toma el final del día local para que el rango sea inclusivo del día tecleado.
        fromStartDate: this.fromDate ? new Date(`${this.fromDate}T00:00:00`).toISOString() : undefined,
        toStartDate: this.toDate ? new Date(`${this.toDate}T23:59:59`).toISOString() : undefined,
      })
      .subscribe({
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
