using EventosVivos.Application.Tests.TestDoubles;
using EventosVivos.Application.UseCases;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Application.Tests.UseCases;

public sealed class CancelReservationUseCaseTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Event CreateEvent(DateTime startDateTime)
    {
        return Event.Create(
            title: "Evento de prueba para cancelación",
            description: "Descripción suficientemente larga para pasar la validación de dominio.",
            venueId: 1,
            venueCapacity: 200,
            maxCapacity: 100,
            startDateTime: startDateTime,
            endDateTime: startDateTime.AddHours(3),
            ticketPrice: 50m,
            type: EventType.Conference,
            currentTime: CurrentTime);
    }

    private static Reservation CreateReservation(Guid eventId, TimeSpan timeUntilEventStart)
    {
        return Reservation.Create(
            eventId: eventId,
            quantity: 2,
            buyerName: "Comprador de prueba",
            buyerEmail: "comprador@example.com",
            availableTickets: 100,
            eventTicketPrice: 50m,
            timeUntilEventStart: timeUntilEventStart,
            currentTime: CurrentTime);
    }

    // RN-07
    [Fact]
    public async Task ExecuteAsync_WhenConfirmedAndEventStartsInLessThan48Hours_MarksReservationAsLost()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent(startDateTime: CurrentTime.AddHours(40));
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);

        var reservations = new InMemoryReservationRepository();
        var reservation = CreateReservation(eventId: targetEvent.Id, timeUntilEventStart: TimeSpan.FromHours(40));
        reservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);
        await reservations.AddAsync(reservation: reservation, cancellationToken: CancellationToken.None);

        var useCase = new CancelReservationUseCase(
            reservationRepository: reservations,
            eventRepository: events,
            clock: new FixedClock(utcNow: CurrentTime));

        var response = await useCase.ExecuteAsync(reservationId: reservation.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "perdida", actual: response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPendingPayment_ReleasesTicketsAsCancelled()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent(startDateTime: CurrentTime.AddDays(10));
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);

        var reservations = new InMemoryReservationRepository();
        var reservation = CreateReservation(eventId: targetEvent.Id, timeUntilEventStart: TimeSpan.FromDays(10));
        await reservations.AddAsync(reservation: reservation, cancellationToken: CancellationToken.None);

        var useCase = new CancelReservationUseCase(
            reservationRepository: reservations,
            eventRepository: events,
            clock: new FixedClock(utcNow: CurrentTime));

        var response = await useCase.ExecuteAsync(reservationId: reservation.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "cancelada", actual: response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfirmedWithMoreThan48Hours_ReleasesTicketsAsCancelled()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent(startDateTime: CurrentTime.AddDays(10));
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);

        var reservations = new InMemoryReservationRepository();
        var reservation = CreateReservation(eventId: targetEvent.Id, timeUntilEventStart: TimeSpan.FromDays(10));
        reservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);
        await reservations.AddAsync(reservation: reservation, cancellationToken: CancellationToken.None);

        var useCase = new CancelReservationUseCase(
            reservationRepository: reservations,
            eventRepository: events,
            clock: new FixedClock(utcNow: CurrentTime));

        var response = await useCase.ExecuteAsync(reservationId: reservation.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "cancelada", actual: response.Status);
    }
}
