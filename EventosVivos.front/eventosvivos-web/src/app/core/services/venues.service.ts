import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { VenueResponse, VenuePayload } from '../models/catalog.model';

// Adaptador del recurso /venues. La lectura la usa cualquier autenticado (dropdowns); el
// CRUD (crear/editar/borrar) es de organizador y lo bloquea la API con la policy 'Organizer'.
// El JWT lo adjunta el interceptor. Responsabilidad única: traducir el envelope ApiResponse.
@Injectable({ providedIn: 'root' })
export class VenuesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/venues`;

  list(): Observable<VenueResponse[]> {
    return this.http
      .get<ApiResponse<VenueResponse[]>>(this.baseUrl)
      .pipe(map((response) => response.data ?? []));
  }

  create(payload: VenuePayload): Observable<VenueResponse> {
    return this.http
      .post<ApiResponse<VenueResponse>>(this.baseUrl, payload)
      .pipe(map((response) => response.data!));
  }

  update(id: number, payload: VenuePayload): Observable<VenueResponse> {
    return this.http
      .put<ApiResponse<VenueResponse>>(`${this.baseUrl}/${id}`, payload)
      .pipe(map((response) => response.data!));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(map(() => undefined));
  }
}
