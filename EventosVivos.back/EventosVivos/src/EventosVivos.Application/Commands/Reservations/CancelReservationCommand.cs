using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Reservations;

// CQRS — Command (RF-05 + RN-07).
public sealed record CancelReservationCommand(
    Guid ReservationId
) : IRequest<ReservationResponse>;