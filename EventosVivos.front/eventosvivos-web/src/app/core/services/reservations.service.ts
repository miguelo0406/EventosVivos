import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { ReservationResponse, ReserveTicketsRequest } from '../models/catalog.model';

// Adaptador del recurso /reservations. Los componentes dependen de este servicio
// (abstracción inyectada por DI), no de HttpClient directamente: inversión de dependencias
// del lado del cliente. El JWT lo adjunta el interceptor.
@Injectable({ providedIn: 'root' })
export class ReservationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reservations`;

  reserve(request: ReserveTicketsRequest): Observable<ReservationResponse> {
    return this.http
      .post<ApiResponse<ReservationResponse>>(this.baseUrl, request)
      .pipe(map((response) => response.data!));
  }

  getById(id: string): Observable<ReservationResponse> {
    return this.http
      .get<ApiResponse<ReservationResponse>>(`${this.baseUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  confirmPayment(id: string): Observable<ReservationResponse> {
    return this.http
      .post<ApiResponse<ReservationResponse>>(`${this.baseUrl}/${id}/confirm-payment`, {})
      .pipe(map((response) => response.data!));
  }

  cancel(id: string): Observable<ReservationResponse> {
    return this.http
      .post<ApiResponse<ReservationResponse>>(`${this.baseUrl}/${id}/cancel`, {})
      .pipe(map((response) => response.data!));
  }
}
