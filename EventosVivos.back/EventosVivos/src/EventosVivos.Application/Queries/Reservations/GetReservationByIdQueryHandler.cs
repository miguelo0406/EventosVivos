using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Queries.Reservations;

internal sealed class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, ReservationResponse>
{
    private readonly IReservationService _reservationService;

    public GetReservationByIdQueryHandler(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public Task<ReservationResponse> Handle(
        GetReservationByIdQuery request,
        CancellationToken cancellationToken
    ) =>
        _reservationService.GetByIdAsync(
            reservationId: request.ReservationId,
            cancellationToken: cancellationToken
        );
}
