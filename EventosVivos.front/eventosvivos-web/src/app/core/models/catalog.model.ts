// DTOs de negocio (espejo de los responses del backend).

export type EventType = 'conferencia' | 'taller' | 'concierto';
export type EventStatus = 'activo' | 'cancelado' | 'completado';
export type ReservationStatus = 'pendiente_pago' | 'confirmada' | 'cancelada' | 'perdida';

export interface VenueResponse {
  id: number;
  name: string;
  capacity: number;
  city: string;
}

export interface VenuePayload {
  name: string;
  capacity: number;
  city: string;
}

export interface EventResponse {
  id: string;
  title: string;
  description: string;
  venueId: number;
  venueName: string;
  maxCapacity: number;
  startDateTime: string;
  endDateTime: string;
  ticketPrice: number;
  type: EventType;
  status: EventStatus;
  createdAt: string;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  venueId: number;
  maxCapacity: number;
  startDateTime: string;
  endDateTime: string;
  ticketPrice: number;
  type: EventType;
}

export interface ReserveTicketsRequest {
  eventId: string;
  quantity: number;
  buyerName: string;
  buyerEmail: string;
}

export interface ReservationResponse {
  id: string;
  eventId: string;
  quantity: number;
  buyerName: string;
  buyerEmail: string;
  status: ReservationStatus;
  confirmationCode?: string | null;
  createdAt: string;
  confirmedAt?: string | null;
  cancelledAt?: string | null;
}

export interface OccupancyReportResponse {
  eventId: string;
  eventTitle: string;
  venueName: string;
  totalSoldTickets: number;
  totalAvailableTickets: number;
  occupancyPercentage: number;
  totalRevenue: number;
  eventStatus: EventStatus;
}

export interface EventFilter {
  type?: EventType;
  status?: EventStatus;
  venueId?: number;
  title?: string;
  // Rango de fecha de inicio (RF-02). ISO UTC: el extremo inferior es inicio del día y el
  // superior fin del día, alineado con el marco horario único UTC de la app.
  fromStartDate?: string;
  toStartDate?: string;
}
