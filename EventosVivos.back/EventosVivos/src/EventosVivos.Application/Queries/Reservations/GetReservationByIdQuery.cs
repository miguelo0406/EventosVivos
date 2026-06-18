using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Queries.Reservations;

// CQRS — Query.
public sealed record GetReservationByIdQuery(
    Guid ReservationId
) : IRequest<ReservationResponse>;