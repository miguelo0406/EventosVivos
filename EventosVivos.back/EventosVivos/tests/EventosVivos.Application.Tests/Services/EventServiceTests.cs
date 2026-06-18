using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Services;
using EventosVivos.Application.Tests.TestDoubles;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Application.Tests.Services;

public sealed class EventServiceTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static (EventService Service, InMemoryEventRepository Events, InMemoryReservationRepository Reservations) CreateSut()
    {
        var venues = new InMemoryVenueRepository(
            seedVenues: [Venue.Create(id: 1, name: "Auditorio Central", capacity: 200, city: "Bogotá")]);
        var events = new InMemoryEventRepository();
        var reservations = new InMemoryReservationRepository();
        var service = new EventService(
            eventRepository: events,
            venueRepository: venues,
            reservationRepository: reservations,
            clock: new FixedClock(utcNow: CurrentTime));

        return (service, events, reservations);
    }

    private static CreateEventRequest CreateValidRequest()
    {
        return new CreateEventRequest
        {
            Title = "Conferencia de Tecnología",
            Description = "Una descripción suficientemente larga para pasar la validación.",
            VenueId = 1,
            MaxCapacity = 100,
            StartDateTime = CurrentTime.AddDays(10),
            EndDateTime = CurrentTime.AddDays(10).AddHours(3),
            TicketPrice = 50m,
            Type = "conferencia",
        };
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsEventAndReturnsActiveResponse()
    {
        var (service, events, _) = CreateSut();

        var response = await service.CreateAsync(request: CreateValidRequest(), cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "activo", actual: response.Status);
        Assert.Equal(expected: "Auditorio Central", actual: response.VenueName);
        var persistedEvent = await events.GetByIdAsync(id: response.Id, cancellationToken: CancellationToken.None);
        Assert.NotNull(persistedEvent);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownVenue_ThrowsVenueNotFoundException()
    {
        var (service, _, _) = CreateSut();
        var request = CreateValidRequest() with { VenueId = 999 };

        await Assert.ThrowsAsync<VenueNotFoundException>(testCode: () => service.CreateAsync(
            request: request,
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidType_ThrowsDomainValidationException()
    {
        var (service, _, _) = CreateSut();
        var request = CreateValidRequest() with { Type = "rumba" };

        await Assert.ThrowsAsync<DomainValidationException>(testCode: () => service.CreateAsync(
            request: request,
            cancellationToken: CancellationToken.None));
    }

    // RN-02
    [Fact]
    public async Task CreateAsync_WithOverlappingScheduleAtSameVenue_ThrowsVenueScheduleConflictException()
    {
        var (service, _, _) = CreateSut();
        var firstRequest = CreateValidRequest();
        await service.CreateAsync(request: firstRequest, cancellationToken: CancellationToken.None);

        var overlappingRequest = firstRequest with
        {
            Title = "Segundo evento superpuesto",
            StartDateTime = firstRequest.StartDateTime.AddHours(1),
            EndDateTime = firstRequest.EndDateTime.AddHours(1),
        };

        await Assert.ThrowsAsync<VenueScheduleConflictException>(testCode: () => service.CreateAsync(
            request: overlappingRequest,
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithNonOverlappingScheduleAtSameVenue_Succeeds()
    {
        var (service, _, _) = CreateSut();
        var firstRequest = CreateValidRequest();
        await service.CreateAsync(request: firstRequest, cancellationToken: CancellationToken.None);

        var secondRequest = firstRequest with
        {
            Title = "Segundo evento sin superposición",
            StartDateTime = firstRequest.EndDateTime.AddHours(1),
            EndDateTime = firstRequest.EndDateTime.AddHours(4),
        };

        var response = await service.CreateAsync(request: secondRequest, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "activo", actual: response.Status);
    }

    // RN-01
    [Fact]
    public async Task CreateAsync_WithCapacityAboveVenueCapacity_ThrowsVenueCapacityExceededException()
    {
        var (service, _, _) = CreateSut();
        var request = CreateValidRequest() with { MaxCapacity = 500 };

        await Assert.ThrowsAsync<VenueCapacityExceededException>(testCode: () => service.CreateAsync(
            request: request,
            cancellationToken: CancellationToken.None));
    }

    // RF-06
    [Fact]
    public async Task GetOccupancyReportAsync_WithConfirmedReservations_ReturnsAggregatedReport()
    {
        var (service, events, reservations) = CreateSut();
        var createResponse = await service.CreateAsync(request: CreateValidRequest(), cancellationToken: CancellationToken.None);

        var targetEvent = await events.GetByIdAsync(id: createResponse.Id, cancellationToken: CancellationToken.None);
        var confirmedReservation = Reservation.Create(
            eventId: targetEvent!.Id,
            quantity: 10,
            buyerName: "Comprador confirmado",
            buyerEmail: "confirmado@example.com",
            availableTickets: 100,
            eventTicketPrice: 50m,
            timeUntilEventStart: TimeSpan.FromDays(10),
            currentTime: CurrentTime);
        confirmedReservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);
        await reservations.AddAsync(reservation: confirmedReservation, cancellationToken: CancellationToken.None);

        var report = await service.GetOccupancyReportAsync(eventId: targetEvent.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: 10, actual: report.TotalSoldTickets);
        Assert.Equal(expected: 90, actual: report.TotalAvailableTickets);
        Assert.Equal(expected: 500m, actual: report.TotalRevenue);
        Assert.Equal(expected: "Auditorio Central", actual: report.VenueName);
        Assert.Equal(expected: "activo", actual: report.EventStatus);
    }
}
