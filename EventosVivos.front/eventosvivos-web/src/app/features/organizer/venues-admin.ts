import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { VenuesService } from '../../core/services/venues.service';
import { ApiResponse } from '../../core/models/api-response.model';
import { VenueResponse } from '../../core/models/catalog.model';

// Superficie de organizador — CRUD de venues (valor agregado). El foco de calidad son los
// "casos borde": el backend rechaza borrar un venue con eventos (409) y reducir su aforo por
// debajo de un evento ya programado (409); aquí se muestran esos errores en el lugar de la
// acción. La validación de forma (longitudes, capacidad positiva) también la impone el dominio.
@Component({
  selector: 'app-venues-admin',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <a routerLink="/admin" class="back muted">← Volver al panel</a>

    <section class="head">
      <div>
        <h1>Lugares (venues)</h1>
        <p class="muted">Administra los lugares donde se programan los eventos.</p>
      </div>
      @if (!showForm()) {
        <button class="btn btn-accent" (click)="startCreate()">Nuevo lugar</button>
      }
    </section>

    @if (actionError()) {
      <div class="alert alert-error">{{ actionError() }}</div>
    }

    @if (showForm()) {
      <form class="card form" [formGroup]="form" (ngSubmit)="submit()">
        <h3>{{ editingId() ? 'Editar lugar' : 'Nuevo lugar' }}</h3>
        @if (formErrors().length) {
          <div class="alert alert-error">
            <ul class="error-list">
              @for (message of formErrors(); track message) {
                <li>{{ message }}</li>
              }
            </ul>
          </div>
        }
        <div class="fields">
          <div class="field grow">
            <label for="name">Nombre</label>
            <input id="name" class="input" type="text" formControlName="name" placeholder="Ej. Teatro Colón" />
          </div>
          <div class="field">
            <label for="capacity">Aforo</label>
            <input id="capacity" class="input" type="number" min="1" formControlName="capacity" />
          </div>
          <div class="field">
            <label for="city">Ciudad</label>
            <input id="city" class="input" type="text" formControlName="city" placeholder="Ej. Bogotá" />
          </div>
        </div>
        <div class="form-actions">
          <button type="button" class="btn btn-ghost" (click)="cancelForm()">Cancelar</button>
          <button type="submit" class="btn btn-accent" [disabled]="saving()">
            {{ saving() ? 'Guardando…' : 'Guardar' }}
          </button>
        </div>
      </form>
    }

    @if (loading()) {
      <p class="muted">Cargando lugares…</p>
    } @else if (loadError()) {
      <div class="alert alert-error">{{ loadError() }}</div>
    } @else {
      <div class="card table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Ciudad</th>
              <th class="num">Aforo</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (venue of venues(); track venue.id) {
              <tr>
                <td class="name">{{ venue.name }}</td>
                <td>{{ venue.city }}</td>
                <td class="num">{{ venue.capacity }}</td>
                <td class="num actions">
                  <button class="btn btn-ghost btn-sm" [disabled]="actingId() === venue.id" (click)="startEdit(venue)">
                    Editar
                  </button>
                  @if (confirmingId() === venue.id) {
                    <button class="btn btn-danger btn-sm" [disabled]="actingId() === venue.id" (click)="remove(venue)">
                      {{ actingId() === venue.id ? 'Eliminando…' : 'Confirmar' }}
                    </button>
                    <button class="btn btn-ghost btn-sm" (click)="confirmingId.set(null)">No</button>
                  } @else {
                    <button class="btn btn-ghost btn-sm" (click)="confirmingId.set(venue.id)">Eliminar</button>
                  }
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
      .back {
        display: inline-block;
        margin-bottom: var(--space-4);
        text-decoration: none;
      }
      .head {
        display: flex;
        align-items: flex-end;
        justify-content: space-between;
        gap: var(--space-4);
        margin-bottom: var(--space-5);
      }
      .form {
        margin-bottom: var(--space-5);
      }
      .form h3 {
        margin-top: 0;
      }
      .fields {
        display: flex;
        gap: var(--space-4);
        flex-wrap: wrap;
      }
      .field.grow {
        flex: 1 1 220px;
      }
      .error-list {
        margin: 0;
        padding-left: var(--space-5);
      }
      .form-actions {
        display: flex;
        justify-content: flex-end;
        gap: var(--space-3);
        margin-top: var(--space-4);
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
      .name {
        font-weight: 600;
      }
      .num {
        text-align: right;
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
    `,
  ],
})
export class VenuesAdminComponent {
  private readonly fb = inject(FormBuilder);
  private readonly venuesService = inject(VenuesService);

  protected readonly venues = signal<VenueResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly showForm = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly formErrors = signal<string[]>([]);

  protected readonly confirmingId = signal<number | null>(null);
  protected readonly actingId = signal<number | null>(null);
  protected readonly actionError = signal<string | null>(null);

  protected readonly form = this.fb.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]),
    capacity: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
    city: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.venuesService.list().subscribe({
      next: (venues) => {
        this.venues.set(venues);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('No pudimos cargar los lugares.');
        this.loading.set(false);
      },
    });
  }

  startCreate(): void {
    this.editingId.set(null);
    this.formErrors.set([]);
    this.form.reset({ name: '', capacity: null, city: '' });
    this.showForm.set(true);
  }

  startEdit(venue: VenueResponse): void {
    this.editingId.set(venue.id);
    this.formErrors.set([]);
    this.form.reset({ name: venue.name, capacity: venue.capacity, city: venue.city });
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.formErrors.set([]);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formErrors.set(['Revisa el nombre (3-150), el aforo (mayor que cero) y la ciudad.']);
      return;
    }

    this.saving.set(true);
    this.formErrors.set([]);
    const value = this.form.getRawValue();
    const payload = { name: value.name.trim(), capacity: value.capacity!, city: value.city.trim() };
    const editingId = this.editingId();
    const request$ = editingId
      ? this.venuesService.update(editingId, payload)
      : this.venuesService.create(payload);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.formErrors.set(this.messagesOf(err, 'No pudimos guardar el lugar.'));
      },
    });
  }

  remove(venue: VenueResponse): void {
    this.actingId.set(venue.id);
    this.actionError.set(null);
    this.venuesService.delete(venue.id).subscribe({
      next: () => {
        this.actingId.set(null);
        this.confirmingId.set(null);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.actingId.set(null);
        this.confirmingId.set(null);
        this.actionError.set(this.messagesOf(err, 'No pudimos eliminar el lugar.')[0]);
      },
    });
  }

  private messagesOf(err: HttpErrorResponse, fallback: string): string[] {
    const body = err.error as ApiResponse<unknown> | undefined;
    const details = body?.error?.details;
    if (details && details.length) {
      return details;
    }
    return [body?.error?.message ?? fallback];
  }
}
