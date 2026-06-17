using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.UseCases;

public sealed class ListReservationsByEventUseCase : IListReservationsByEventUseCase
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IEventRepository _eventRepository;

    public ListReservationsByEventUseCase(
        IReservationRepository reservationRepository,
        IEventRepository eventRepository)
    {
        _reservationRepository = reservationRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IReadOnlyList<ReservationResponse>> ExecuteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        _ = await _eventRepository.GetByIdAsync(id: eventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: eventId);

        var reservations = await _reservationRepository.GetByEventIdAsync(
            eventId: eventId,
            cancellationToken: cancellationToken);

        return reservations.Select(ReservationResponseMapper.ToResponse).ToList();
    }
}
