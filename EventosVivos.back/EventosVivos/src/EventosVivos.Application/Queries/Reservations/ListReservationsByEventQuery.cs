using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Queries.Reservations;

// CQRS — Query: reservas de un evento (soporte a RF-04/RF-05 desde el panel admin).
public sealed record ListReservationsByEventQuery(
    Guid EventId
) : IRequest<IReadOnlyList<ReservationResponse>>;