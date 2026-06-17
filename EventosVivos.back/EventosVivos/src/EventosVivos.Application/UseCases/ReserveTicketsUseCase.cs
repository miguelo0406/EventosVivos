using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Ports;
using EventosVivos.Domain.Services;

namespace EventosVivos.Application.UseCases;

// RF-03. Responsabilidad única: orquesta la reserva (cargar evento, calcular cupo
// disponible, delegar las invariantes de cantidad/horario a Reservation.Create).
public sealed class ReserveTicketsUseCase : IReserveTicketsUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;

    public ReserveTicketsUseCase(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _clock = clock;
    }

    public async Task<ReservationResponse> ExecuteAsync(ReserveTicketsRequest request, CancellationToken cancellationToken)
    {
        var targetEvent = await _eventRepository.GetByIdAsync(id: request.EventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: request.EventId);

        var currentTime = _clock.UtcNow;

        var effectiveStatus = targetEvent.GetEffectiveStatus(currentTime: currentTime);
        if (effectiveStatus != EventStatus.Active)
        {
            throw new InvalidEventStateException(
                message: $"No se pueden reservar entradas para un evento en estado '{effectiveStatus}'.");
        }

        var existingReservations = await _reservationRepository.GetByEventIdAsync(
            eventId: request.EventId,
            cancellationToken: cancellationToken);

        var availableTickets = TicketAvailabilityCalculator.GetAvailableTickets(
            maxCapacity: targetEvent.MaxCapacity,
            existingReservations: existingReservations);

        var reservation = Reservation.Create(
            eventId: targetEvent.Id,
            quantity: request.Quantity,
            buyerName: request.BuyerName,
            buyerEmail: request.BuyerEmail,
            availableTickets: availableTickets,
            eventTicketPrice: targetEvent.TicketPrice,
            timeUntilEventStart: targetEvent.GetTimeUntilStart(currentTime: currentTime),
            currentTime: currentTime);

        await _reservationRepository.AddAsync(reservation: reservation, cancellationToken: cancellationToken);
        await _reservationRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }
}
