using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Reservations;

// CQRS — Command (RF-03).
public sealed record ReserveTicketsCommand(
    ReserveTicketsRequest Request
) : IRequest<ReservationResponse>;