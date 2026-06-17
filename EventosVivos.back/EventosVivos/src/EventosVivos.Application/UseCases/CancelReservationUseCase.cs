using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.UseCases;

// RF-05 + RN-07. La penalización de "perdida" se decide dentro de
// Reservation.Cancel; este caso de uso solo orquesta la carga de las dos entidades
// involucradas (Reservation y Event, para conocer cuánto falta para el evento).
public sealed class CancelReservationUseCase : ICancelReservationUseCase
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public CancelReservationUseCase(
        IReservationRepository reservationRepository,
        IEventRepository eventRepository,
        IClock clock)
    {
        _reservationRepository = reservationRepository;
        _eventRepository = eventRepository;
        _clock = clock;
    }

    public async Task<ReservationResponse> ExecuteAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id: reservationId, cancellationToken: cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId: reservationId);

        var targetEvent = await _eventRepository.GetByIdAsync(id: reservation.EventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: reservation.EventId);

        var currentTime = _clock.UtcNow;

        reservation.Cancel(
            currentTime: currentTime,
            timeUntilEventStart: targetEvent.GetTimeUntilStart(currentTime: currentTime));

        await _reservationRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }
}
