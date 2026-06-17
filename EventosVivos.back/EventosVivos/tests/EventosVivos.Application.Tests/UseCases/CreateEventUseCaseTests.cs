using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Tests.TestDoubles;
using EventosVivos.Application.UseCases;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Tests.UseCases;

public sealed class CreateEventUseCaseTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static (CreateEventUseCase UseCase, InMemoryEventRepository Events) CreateSut()
    {
        var venues = new InMemoryVenueRepository(
            seedVenues: [Venue.Create(id: 1, name: "Auditorio Central", capacity: 200, city: "Bogotá")]);
        var events = new InMemoryEventRepository();
        var useCase = new CreateEventUseCase(
            eventRepository: events,
            venueRepository: venues,
            clock: new FixedClock(utcNow: CurrentTime));

        return (useCase, events);
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
    public async Task ExecuteAsync_WithValidRequest_PersistsEventAndReturnsActiveResponse()
    {
        var (useCase, events) = CreateSut();

        var response = await useCase.ExecuteAsync(request: CreateValidRequest(), cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "activo", actual: response.Status);
        Assert.Equal(expected: "Auditorio Central", actual: response.VenueName);
        var persistedEvent = await events.GetByIdAsync(id: response.Id, cancellationToken: CancellationToken.None);
        Assert.NotNull(persistedEvent);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownVenue_ThrowsVenueNotFoundException()
    {
        var (useCase, _) = CreateSut();
        var request = CreateValidRequest() with { VenueId = 999 };

        await Assert.ThrowsAsync<VenueNotFoundException>(testCode: () => useCase.ExecuteAsync(
            request: request,
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidType_ThrowsDomainValidationException()
    {
        var (useCase, _) = CreateSut();
        var request = CreateValidRequest() with { Type = "rumba" };

        await Assert.ThrowsAsync<DomainValidationException>(testCode: () => useCase.ExecuteAsync(
            request: request,
            cancellationToken: CancellationToken.None));
    }

    // RN-02
    [Fact]
    public async Task ExecuteAsync_WithOverlappingScheduleAtSameVenue_ThrowsVenueScheduleConflictException()
    {
        var (useCase, _) = CreateSut();
        var firstRequest = CreateValidRequest();
        await useCase.ExecuteAsync(request: firstRequest, cancellationToken: CancellationToken.None);

        var overlappingRequest = firstRequest with
        {
            Title = "Segundo evento superpuesto",
            StartDateTime = firstRequest.StartDateTime.AddHours(1),
            EndDateTime = firstRequest.EndDateTime.AddHours(1),
        };

        await Assert.ThrowsAsync<VenueScheduleConflictException>(testCode: () => useCase.ExecuteAsync(
            request: overlappingRequest,
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonOverlappingScheduleAtSameVenue_Succeeds()
    {
        var (useCase, _) = CreateSut();
        var firstRequest = CreateValidRequest();
        await useCase.ExecuteAsync(request: firstRequest, cancellationToken: CancellationToken.None);

        var secondRequest = firstRequest with
        {
            Title = "Segundo evento sin superposición",
            StartDateTime = firstRequest.EndDateTime.AddHours(1),
            EndDateTime = firstRequest.EndDateTime.AddHours(4),
        };

        var response = await useCase.ExecuteAsync(request: secondRequest, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "activo", actual: response.Status);
    }

    // RN-01
    [Fact]
    public async Task ExecuteAsync_WithCapacityAboveVenueCapacity_ThrowsVenueCapacityExceededException()
    {
        var (useCase, _) = CreateSut();
        var request = CreateValidRequest() with { MaxCapacity = 500 };

        await Assert.ThrowsAsync<VenueCapacityExceededException>(testCode: () => useCase.ExecuteAsync(
            request: request,
            cancellationToken: CancellationToken.None));
    }
}
