using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Services.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Ports;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Application.Services;

// Responsabilidad única (S de SOLID): orquestación de los casos de uso del agregado
// Reservation (RF-03 a RF-05). Las invariantes (límites de entradas, transiciones de
// estado, penalización RN-07) viven en Reservation; este servicio solo coordina.
public sealed class ReservationService : IReservationService
{
    private const int MaxConfirmationCodeAttempts = 5;

    private readonly IReservationRepository _reservationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public ReservationService(
        IReservationRepository reservationRepository,
        IEventRepository eventRepository,
        IClock clock)
    {
        _reservationRepository = reservationRepository;
        _eventRepository = eventRepository;
        _clock = clock;
    }

    // RF-03.
    public async Task<ReservationResponse> ReserveAsync(
        ReserveTicketsRequest request,
        CancellationToken cancellationToken
    )
    {
        var targetEvent = await _eventRepository.GetByIdAsync(id: request.EventId, cancellationToken: cancellationToken)
                          ?? throw new EventNotFoundException(eventId: request.EventId);

        var currentTime = _clock.UtcNow;

        var effectiveStatus = targetEvent.GetEffectiveStatus(currentTime: currentTime);
        if (effectiveStatus != EventStatus.Active)
        {
            throw new InvalidEventStateException(
                message: $"No se pueden reservar entradas para un evento en estado '{effectiveStatus}'."
            );
        }

        var existingReservations = await _reservationRepository.GetByEventIdAsync(
            eventId: request.EventId,
            cancellationToken: cancellationToken
        );

        var availableTickets = TicketAvailabilityCalculator.GetAvailableTickets(
            maxCapacity: targetEvent.MaxCapacity,
            existingReservations: existingReservations
        );

        var reservation = Reservation.Create(
            eventId: targetEvent.Id,
            quantity: request.Quantity,
            buyerName: request.BuyerName,
            buyerEmail: request.BuyerEmail,
            availableTickets: availableTickets,
            eventTicketPrice: targetEvent.TicketPrice,
            timeUntilEventStart: targetEvent.GetTimeUntilStart(currentTime: currentTime),
            currentTime: currentTime
        );

        await _reservationRepository.AddAsync(reservation: reservation, cancellationToken: cancellationToken);

        await _reservationRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }

    public async Task<ReservationResponse> GetByIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken
    )
    {
        var reservation =
            await _reservationRepository.GetByIdAsync(id: reservationId, cancellationToken: cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId: reservationId);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }

    // RF-04.
    public async Task<ReservationResponse> ConfirmPaymentAsync(
        Guid reservationId,
        CancellationToken cancellationToken
    )
    {
        var reservation =
            await _reservationRepository.GetByIdAsync(id: reservationId, cancellationToken: cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId: reservationId);

        var confirmationCode = await GenerateUniqueConfirmationCodeAsync(cancellationToken: cancellationToken);

        reservation.ConfirmPayment(confirmationCode: confirmationCode, currentTime: _clock.UtcNow);

        await _reservationRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }

    // RF-05 + RN-07.
    public async Task<ReservationResponse> CancelAsync(
        Guid reservationId,
        CancellationToken cancellationToken
    )
    {
        var reservation =
            await _reservationRepository.GetByIdAsync(id: reservationId, cancellationToken: cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId: reservationId);

        var targetEvent =
            await _eventRepository.GetByIdAsync(id: reservation.EventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: reservation.EventId);

        var currentTime = _clock.UtcNow;

        reservation.Cancel(
            currentTime: currentTime,
            timeUntilEventStart: targetEvent.GetTimeUntilStart(currentTime: currentTime));

        await _reservationRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }

    public async Task<IReadOnlyList<ReservationResponse>> GetByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken
    )
    {
        _ = await _eventRepository.GetByIdAsync(id: eventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: eventId);

        var reservations = await _reservationRepository.GetByEventIdAsync(
            eventId: eventId,
            cancellationToken: cancellationToken);

        return reservations.Select(ReservationResponseMapper.ToResponse).ToList();
    }

    // RF-04: garantiza la unicidad del código (Domain no puede consultar el repositorio).
    private async Task<ConfirmationCode> GenerateUniqueConfirmationCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxConfirmationCodeAttempts; attempt++)
        {
            var candidate = ConfirmationCode.Generate();
            var alreadyExists = await _reservationRepository.ExistsByConfirmationCodeAsync(
                confirmationCode: candidate.Value,
                cancellationToken: cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(message: "No fue posible generar un código de reserva único.");
    }
}