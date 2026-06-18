import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { EventsService } from '../../core/services/events.service';
import { VenuesService } from '../../core/services/venues.service';
import { ApiResponse } from '../../core/models/api-response.model';
import { EventType, VenueResponse } from '../../core/models/catalog.model';

// Superficie de organizador — creación de evento (RF-01). El formulario replica las
// validaciones del dominio (longitudes, fechas futuras, precio/aforo positivos) para dar
// feedback inmediato. Las reglas que dependen del estado del servidor (RN-01 aforo del
// venue, RN-02 solape de horario, RN-03 noche de fin de semana) se muestran con el detalle
// de error que devuelve la API en el mismo envelope ApiResponse.
@Component({
  selector: 'app-event-create',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <a routerLink="/admin" class="back muted">← Volver al panel</a>

    <section class="head">
      <h1>Crear evento</h1>
      <p class="muted">Define el lugar, el horario y el aforo. Las reglas de negocio se validan al guardar.</p>
    </section>

    @if (errors().length) {
      <div class="alert alert-error">
        @if (errors().length === 1) {
          {{ errors()[0] }}
        } @else {
          <ul class="error-list">
            @for (message of errors(); track message) {
              <li>{{ message }}</li>
            }
          </ul>
        }
      </div>
    }

    <form class="card form-grid" [formGroup]="form" (ngSubmit)="submit()">
      <div class="field span-2">
        <label for="title">Título</label>
        <input id="title" class="input" type="text" formControlName="title" placeholder="Ej. Arquitectura del sonido en vivo" />
        @if (invalid('title')) {
          <small class="err">Entre 5 y 100 caracteres.</small>
        }
      </div>

      <div class="field span-2">
        <label for="description">Descripción</label>
        <textarea id="description" class="input" rows="3" formControlName="description"
                  placeholder="De qué trata, a quién va dirigido…"></textarea>
        @if (invalid('description')) {
          <small class="err">Entre 10 y 500 caracteres.</small>
        }
      </div>

      <div class="field">
        <label for="type">Tipo</label>
        <select id="type" class="input" formControlName="type">
          <option value="conferencia">Conferencia</option>
          <option value="taller">Taller</option>
          <option value="concierto">Concierto</option>
        </select>
      </div>

      <div class="field">
        <label for="venue">Lugar</label>
        <select id="venue" class="input" formControlName="venueId">
          <option [ngValue]="null" disabled>Selecciona un lugar…</option>
          @for (venue of venues(); track venue.id) {
            <option [ngValue]="venue.id">{{ venue.name }} — {{ venue.city }} · aforo {{ venue.capacity }}</option>
          }
        </select>
        @if (invalid('venueId')) {
          <small class="err">Selecciona el lugar del evento.</small>
        }
      </div>

      <div class="field">
        <label for="capacity">Aforo del evento</label>
        <input id="capacity" class="input" type="number" min="1" [attr.max]="selectedVenue()?.capacity ?? null"
               formControlName="maxCapacity" />
        @if (selectedVenue(); as venue) {
          <small class="muted">Máximo del lugar: {{ venue.capacity }} (RN-01).</small>
        }
        @if (invalid('maxCapacity')) {
          <small class="err">Indica un aforo mayor que cero.</small>
        }
      </div>

      <div class="field">
        <label for="price">Precio de entrada (USD)</label>
        <input id="price" class="input" type="number" min="0.01" step="0.01" formControlName="ticketPrice" />
        @if (invalid('ticketPrice')) {
          <small class="err">El precio debe ser mayor que cero.</small>
        }
      </div>

      <div class="field">
        <label for="start">Inicio</label>
        <input id="start" class="input" type="datetime-local" [min]="minDateTime" formControlName="startLocal" />
        @if (invalid('startLocal')) {
          <small class="err">Indica la fecha y hora de inicio.</small>
        }
      </div>

      <div class="field">
        <label for="end">Fin</label>
        <input id="end" class="input" type="datetime-local" [min]="minDateTime" formControlName="endLocal" />
        @if (invalid('endLocal')) {
          <small class="err">Indica la fecha y hora de fin.</small>
        }
      </div>

      <p class="hint muted span-2">
        Horario en tu hora local (el navegador la detecta automáticamente). Los eventos de sábado
        o domingo no pueden iniciar a las 22:00 o después en tu hora local (RN-03), y no puede
        haber otro evento activo en el mismo lugar con horario superpuesto (RN-02).
      </p>

      <div class="actions span-2">
        <a routerLink="/admin" class="btn btn-ghost">Cancelar</a>
        <button type="submit" class="btn btn-accent" [disabled]="submitting()">
          {{ submitting() ? 'Creando…' : 'Crear evento' }}
        </button>
      </div>
    </form>
  `,
  styles: [
    `
      .back {
        display: inline-block;
        margin-bottom: var(--space-4);
        text-decoration: none;
      }
      .head {
        margin-bottom: var(--space-5);
      }
      .form-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: var(--space-5);
      }
      .span-2 {
        grid-column: 1 / -1;
      }
      textarea.input {
        resize: vertical;
        min-height: 84px;
      }
      .err {
        color: var(--danger);
        font-size: 0.8rem;
      }
      .hint {
        font-size: 0.85rem;
        margin: 0;
      }
      .error-list {
        margin: 0;
        padding-left: var(--space-5);
      }
      .actions {
        display: flex;
        justify-content: flex-end;
        gap: var(--space-3);
      }
      .actions .btn-ghost {
        text-decoration: none;
      }
      @media (max-width: 640px) {
        .form-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class EventCreateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly eventsService = inject(EventsService);
  private readonly venuesService = inject(VenuesService);

  protected readonly venues = signal<VenueResponse[]>([]);
  protected readonly submitting = signal(false);
  protected readonly errors = signal<string[]>([]);

  // "Ahora" en hora local (no UTC): el input datetime-local no lleva zona horaria, así que el
  // atributo min debe expresarse en el mismo marco que el navegador usa para mostrar el valor.
  protected readonly minDateTime = EventCreateComponent.toLocalInputValue(new Date());

  // Strings no anulables (nonNullable) y numéricos anulables (arrancan vacíos hasta que el
  // organizador los completa): así getRawValue() entrega tipos limpios para el request.
  protected readonly form = this.fb.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(5), Validators.maxLength(100)]),
    description: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]),
    type: this.fb.nonNullable.control('conferencia', [Validators.required]),
    venueId: this.fb.control<number | null>(null, [Validators.required]),
    maxCapacity: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
    ticketPrice: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.01)]),
    startLocal: this.fb.nonNullable.control('', [Validators.required]),
    endLocal: this.fb.nonNullable.control('', [Validators.required]),
  });

  constructor() {
    this.venuesService.list().subscribe((venues) => this.venues.set(venues));
  }

  protected invalid(controlName: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.invalid;
  }

  protected selectedVenue(): VenueResponse | undefined {
    const venueId = this.form.controls.venueId.value;
    return venueId == null ? undefined : this.venues().find((venue) => venue.id === venueId);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errors.set([]);
    const value = this.form.getRawValue();

    this.eventsService
      .create({
        title: value.title.trim(),
        description: value.description.trim(),
        venueId: value.venueId!,
        maxCapacity: value.maxCapacity!,
        ticketPrice: value.ticketPrice!,
        type: value.type as EventType,
        startDateTime: this.toUtcIso(value.startLocal),
        endDateTime: this.toUtcIso(value.endLocal),
      })
      .subscribe({
        next: (created) => this.router.navigate(['/admin/events', created.id]),
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.errors.set(this.messagesOf(err));
        },
      });
  }

  // El navegador entrega datetime-local sin zona horaria; Date lo interpreta como hora LOCAL
  // del usuario (estándar HTML5) y toISOString() la convierte al UTC real equivalente. Así el
  // organizador piensa siempre en su hora local y RN-03 evalúa esa misma hora ya convertida.
  private toUtcIso(localValue: string): string {
    return new Date(localValue).toISOString();
  }

  // Formatea un Date a "YYYY-MM-DDTHH:mm" en hora LOCAL (no UTC), el formato que exige el
  // atributo min de un input datetime-local para no desalinearse con lo que el usuario ve.
  private static toLocalInputValue(date: Date): string {
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  private messagesOf(err: HttpErrorResponse): string[] {
    const body = err.error as ApiResponse<unknown> | undefined;
    const details = body?.error?.details;
    if (details && details.length) {
      return details;
    }
    if (body?.error?.message) {
      return [body.error.message];
    }
    return ['No pudimos crear el evento.'];
  }
}
