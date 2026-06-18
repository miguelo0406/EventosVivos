using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Services;
using EventosVivos.Application.Tests.TestDoubles;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Tests.Services;

public sealed class ReservationServiceTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Event CreateEvent(DateTime? startDateTime = null, int maxCapacity = 100, decimal ticketPrice = 50m)
    {
        var start = startDateTime ?? CurrentTime.AddDays(10);

        return Event.Create(
            title: "Evento de prueba para reservas",
            description: "Descripción suficientemente larga para pasar la validación de dominio.",
            venueId: 1,
            venueCapacity: 200,
            maxCapacity: maxCapacity,
            startDateTime: start,
            endDateTime: start.AddHours(3),
            ticketPrice: ticketPrice,
            type: EventType.Conference,
            currentTime: CurrentTime);
    }

    private static ReservationService CreateSut(InMemoryEventRepository events, InMemoryReservationRepository reservations)
    {
        return new ReservationService(
            reservationRepository: reservations,
            eventRepository: events,
            clock: new FixedClock(utcNow: CurrentTime));
    }

    private static ReserveTicketsRequest CreateValidRequest(Guid eventId, int quantity = 3)
    {
        return new ReserveTicketsRequest
        {
            EventId = eventId,
            Quantity = quantity,
            BuyerName = "Ana Gómez",
            BuyerEmail = "ana.gomez@example.com",
        };
    }

    // RF-03
    [Fact]
    public async Task ReserveAsync_WithValidRequest_CreatesPendingPaymentReservation()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent();
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var service = CreateSut(events: events, reservations: new InMemoryReservationRepository());

        var response = await service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "pendiente_pago", actual: response.Status);
    }

    [Fact]
    public async Task ReserveAsync_ForCancelledEvent_ThrowsInvalidEventStateException()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent();
        targetEvent.Cancel(currentTime: CurrentTime);
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var service = CreateSut(events: events, reservations: new InMemoryReservationRepository());

        await Assert.ThrowsAsync<InvalidEventStateException>(testCode: () => service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ReserveAsync_WhenQuantityExceedsRemainingCapacity_ThrowsInsufficientTicketsAvailableException()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent(maxCapacity: 5);
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var service = CreateSut(events: events, reservations: new InMemoryReservationRepository());

        await service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id, quantity: 4),
            cancellationToken: CancellationToken.None);

        await Assert.ThrowsAsync<InsufficientTicketsAvailableException>(testCode: () => service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id, quantity: 2),
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ReserveAsync_WithUnknownEvent_ThrowsEventNotFoundException()
    {
        var service = CreateSut(events: new InMemoryEventRepository(), reservations: new InMemoryReservationRepository());

        await Assert.ThrowsAsync<EventNotFoundException>(testCode: () => service.ReserveAsync(
            request: CreateValidRequest(eventId: Guid.NewGuid()),
            cancellationToken: CancellationToken.None));
    }

    // RF-04
    [Fact]
    public async Task ConfirmPaymentAsync_WithPendingReservation_GeneratesConfirmationCode()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent();
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var reservations = new InMemoryReservationRepository();
        var service = CreateSut(events: events, reservations: reservations);
        var reserved = await service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None);

        var response = await service.ConfirmPaymentAsync(reservationId: reserved.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "confirmada", actual: response.Status);
        Assert.NotNull(response.ConfirmationCode);
        Assert.Matches(expectedRegexPattern: "^EV-\\d{6}$", actualString: response.ConfirmationCode);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WhenAlreadyConfirmed_ThrowsInvalidReservationStateTransitionException()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent();
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var reservations = new InMemoryReservationRepository();
        var service = CreateSut(events: events, reservations: reservations);
        var reserved = await service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None);
        await service.ConfirmPaymentAsync(reservationId: reserved.Id, cancellationToken: CancellationToken.None);

        await Assert.ThrowsAsync<InvalidReservationStateTransitionException>(testCode: () => service.ConfirmPaymentAsync(
            reservationId: reserved.Id,
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WithUnknownReservation_ThrowsReservationNotFoundException()
    {
        var service = CreateSut(events: new InMemoryEventRepository(), reservations: new InMemoryReservationRepository());

        await Assert.ThrowsAsync<ReservationNotFoundException>(testCode: () => service.ConfirmPaymentAsync(
            reservationId: Guid.NewGuid(),
            cancellationToken: CancellationToken.None));
    }

    // RN-07
    [Fact]
    public async Task CancelAsync_WhenConfirmedAndEventStartsInLessThan48Hours_MarksReservationAsLost()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent(startDateTime: CurrentTime.AddHours(40));
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var reservations = new InMemoryReservationRepository();
        var service = CreateSut(events: events, reservations: reservations);
        var reserved = await service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None);
        await service.ConfirmPaymentAsync(reservationId: reserved.Id, cancellationToken: CancellationToken.None);

        var response = await service.CancelAsync(reservationId: reserved.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "perdida", actual: response.Status);
    }

    [Fact]
    public async Task CancelAsync_WhenPendingPayment_ReleasesTicketsAsCancelled()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent();
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var reservations = new InMemoryReservationRepository();
        var service = CreateSut(events: events, reservations: reservations);
        var reserved = await service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None);

        var response = await service.CancelAsync(reservationId: reserved.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "cancelada", actual: response.Status);
    }

    [Fact]
    public async Task CancelAsync_WhenConfirmedWithMoreThan48Hours_ReleasesTicketsAsCancelled()
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent(startDateTime: CurrentTime.AddDays(10));
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);
        var reservations = new InMemoryReservationRepository();
        var service = CreateSut(events: events, reservations: reservations);
        var reserved = await service.ReserveAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None);
        await service.ConfirmPaymentAsync(reservationId: reserved.Id, cancellationToken: CancellationToken.None);

        var response = await service.CancelAsync(reservationId: reserved.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "cancelada", actual: response.Status);
    }
}
