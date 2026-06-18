using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Queries.Reservations;

internal sealed class ListReservationsByEventQueryHandler
    : IRequestHandler<ListReservationsByEventQuery, IReadOnlyList<ReservationResponse>>
{
    private readonly IReservationService _reservationService;

    public ListReservationsByEventQueryHandler(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public Task<IReadOnlyList<ReservationResponse>> Handle(
        ListReservationsByEventQuery request,
        CancellationToken cancellationToken
    ) =>
        _reservationService.GetByEventAsync(
            eventId: request.EventId,
            cancellationToken: cancellationToken
        );
}
