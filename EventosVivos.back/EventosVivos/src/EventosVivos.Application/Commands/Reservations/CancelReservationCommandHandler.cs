using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Reservations;

internal sealed class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, ReservationResponse>
{
    private readonly IReservationService _reservationService;

    public CancelReservationCommandHandler(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public Task<ReservationResponse> Handle(
        CancelReservationCommand request,
        CancellationToken cancellationToken
    ) =>
        _reservationService.CancelAsync(
            reservationId: request.ReservationId,
            cancellationToken: cancellationToken
        );
}
