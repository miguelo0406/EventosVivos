using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Tests.TestDoubles;
using EventosVivos.Application.UseCases;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Tests.UseCases;

public sealed class ReserveTicketsUseCaseTests
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

    private static async Task<(ReserveTicketsUseCase UseCase, InMemoryEventRepository Events, Event Event)> CreateSutAsync(
        int maxCapacity = 100,
        bool cancelEvent = false)
    {
        var events = new InMemoryEventRepository();
        var targetEvent = CreateEvent(maxCapacity: maxCapacity);
        if (cancelEvent)
        {
            targetEvent.Cancel(currentTime: CurrentTime);
        }

        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);

        var useCase = new ReserveTicketsUseCase(
            eventRepository: events,
            reservationRepository: new InMemoryReservationRepository(),
            clock: new FixedClock(utcNow: CurrentTime));

        return (useCase, events, targetEvent);
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

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_CreatesPendingPaymentReservation()
    {
        var (useCase, _, targetEvent) = await CreateSutAsync();

        var response = await useCase.ExecuteAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "pendiente_pago", actual: response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ForCancelledEvent_ThrowsInvalidEventStateException()
    {
        var (useCase, _, targetEvent) = await CreateSutAsync(cancelEvent: true);

        await Assert.ThrowsAsync<InvalidEventStateException>(testCode: () => useCase.ExecuteAsync(
            request: CreateValidRequest(eventId: targetEvent.Id),
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenQuantityExceedsRemainingCapacity_ThrowsInsufficientTicketsAvailableException()
    {
        var (useCase, _, targetEvent) = await CreateSutAsync(maxCapacity: 5);

        await useCase.ExecuteAsync(
            request: CreateValidRequest(eventId: targetEvent.Id, quantity: 4),
            cancellationToken: CancellationToken.None);

        await Assert.ThrowsAsync<InsufficientTicketsAvailableException>(testCode: () => useCase.ExecuteAsync(
            request: CreateValidRequest(eventId: targetEvent.Id, quantity: 2),
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownEvent_ThrowsEventNotFoundException()
    {
        var (useCase, _, _) = await CreateSutAsync();

        await Assert.ThrowsAsync<EventNotFoundException>(testCode: () => useCase.ExecuteAsync(
            request: CreateValidRequest(eventId: Guid.NewGuid()),
            cancellationToken: CancellationToken.None));
    }
}
