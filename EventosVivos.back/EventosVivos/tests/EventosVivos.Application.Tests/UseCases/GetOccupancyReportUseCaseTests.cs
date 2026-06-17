using EventosVivos.Application.Tests.TestDoubles;
using EventosVivos.Application.UseCases;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Application.Tests.UseCases;

public sealed class GetOccupancyReportUseCaseTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_WithConfirmedReservations_ReturnsAggregatedReport()
    {
        var venues = new InMemoryVenueRepository(
            seedVenues: [Venue.Create(id: 1, name: "Auditorio Central", capacity: 200, city: "Bogotá")]);

        var events = new InMemoryEventRepository();
        var targetEvent = Event.Create(
            title: "Evento para reporte de ocupación",
            description: "Descripción suficientemente larga para pasar la validación de dominio.",
            venueId: 1,
            venueCapacity: 200,
            maxCapacity: 100,
            startDateTime: CurrentTime.AddDays(5),
            endDateTime: CurrentTime.AddDays(5).AddHours(3),
            ticketPrice: 40m,
            type: EventType.Conference,
            currentTime: CurrentTime);
        await events.AddAsync(eventToAdd: targetEvent, cancellationToken: CancellationToken.None);

        var reservations = new InMemoryReservationRepository();
        var confirmedReservation = Reservation.Create(
            eventId: targetEvent.Id,
            quantity: 10,
            buyerName: "Comprador confirmado",
            buyerEmail: "confirmado@example.com",
            availableTickets: 100,
            eventTicketPrice: 40m,
            timeUntilEventStart: TimeSpan.FromDays(5),
            currentTime: CurrentTime);
        confirmedReservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);
        await reservations.AddAsync(reservation: confirmedReservation, cancellationToken: CancellationToken.None);

        var useCase = new GetOccupancyReportUseCase(
            eventRepository: events,
            reservationRepository: reservations,
            venueRepository: venues,
            clock: new FixedClock(utcNow: CurrentTime));

        var report = await useCase.ExecuteAsync(eventId: targetEvent.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: 10, actual: report.TotalSoldTickets);
        Assert.Equal(expected: 90, actual: report.TotalAvailableTickets);
        Assert.Equal(expected: 400m, actual: report.TotalRevenue);
        Assert.Equal(expected: "Auditorio Central", actual: report.VenueName);
        Assert.Equal(expected: "activo", actual: report.EventStatus);
    }
}
