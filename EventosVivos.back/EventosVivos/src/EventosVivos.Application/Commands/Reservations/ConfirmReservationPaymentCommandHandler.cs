using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Reservations;

internal sealed class ConfirmReservationPaymentCommandHandler
    : IRequestHandler<ConfirmReservationPaymentCommand, ReservationResponse>
{
    private readonly IReservationService _reservationService;

    public ConfirmReservationPaymentCommandHandler(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public Task<ReservationResponse> Handle(
        ConfirmReservationPaymentCommand request,
        CancellationToken cancellationToken
    ) =>
        _reservationService.ConfirmPaymentAsync(
            reservationId: request.ReservationId,
            cancellationToken: cancellationToken
        );
}
