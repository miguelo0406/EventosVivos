using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Reservations;

internal sealed class ReserveTicketsCommandHandler : IRequestHandler<ReserveTicketsCommand, ReservationResponse>
{
    private readonly IReservationService _reservationService;

    public ReserveTicketsCommandHandler(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public Task<ReservationResponse> Handle(
        ReserveTicketsCommand request,
        CancellationToken cancellationToken
    ) =>
        _reservationService.ReserveAsync(
            request: request.Request,
            cancellationToken: cancellationToken
        );
}
