using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Reservations;

// CQRS — Command (RF-04).
public sealed record ConfirmReservationPaymentCommand(
    Guid ReservationId
) : IRequest<ReservationResponse>;