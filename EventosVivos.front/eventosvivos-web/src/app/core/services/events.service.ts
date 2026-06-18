import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  CreateEventRequest,
  EventFilter,
  EventResponse,
  OccupancyReportResponse,
  ReservationResponse,
} from '../models/catalog.model';

// Adaptador del recurso /events. El token JWT lo adjunta el interceptor.
@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/events`;

  list(filter: EventFilter = {}): Observable<EventResponse[]> {
    let params = new HttpParams();
    if (filter.type) params = params.set('type', filter.type);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.venueId) params = params.set('venueId', filter.venueId);
    if (filter.title) params = params.set('title', filter.title);
    if (filter.fromStartDate) params = params.set('fromStartDate', filter.fromStartDate);
    if (filter.toStartDate) params = params.set('toStartDate', filter.toStartDate);

    return this.http
      .get<ApiResponse<EventResponse[]>>(this.baseUrl, { params })
      .pipe(map((response) => response.data ?? []));
  }

  getById(id: string): Observable<EventResponse> {
    return this.http
      .get<ApiResponse<EventResponse>>(`${this.baseUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  create(request: CreateEventRequest): Observable<EventResponse> {
    return this.http
      .post<ApiResponse<EventResponse>>(this.baseUrl, request)
      .pipe(map((response) => response.data!));
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, {}).pipe(map(() => undefined));
  }

  occupancyReport(id: string): Observable<OccupancyReportResponse> {
    return this.http
      .get<ApiResponse<OccupancyReportResponse>>(`${this.baseUrl}/${id}/occupancy-report`)
      .pipe(map((response) => response.data!));
  }

  reservations(id: string): Observable<ReservationResponse[]> {
    return this.http
      .get<ApiResponse<ReservationResponse[]>>(`${this.baseUrl}/${id}/reservations`)
      .pipe(map((response) => response.data ?? []));
  }
}
