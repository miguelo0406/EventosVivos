using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Tests;

public sealed class OccupancyReportCalculatorTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Event CreateEvent(DateTime startDateTime, DateTime endDateTime, decimal ticketPrice = 50m)
    {
        return Event.Create(
            title: "Evento de prueba para reporte",
            description: "Descripción suficientemente larga para pasar la validación de dominio.",
            venueId: 1,
            venueCapacity: 200,
            maxCapacity: 100,
            startDateTime: startDateTime,
            endDateTime: endDateTime,
            ticketPrice: ticketPrice,
            type: EventType.Conference,
            currentTime: CurrentTime);
    }

    private static Reservation CreateReservation(Guid eventId, int quantity, decimal eventTicketPrice)
    {
        return Reservation.Create(
            eventId: eventId,
            quantity: quantity,
            buyerName: "Comprador de prueba",
            buyerEmail: $"{Guid.NewGuid():N}@example.com",
            availableTickets: 1000,
            eventTicketPrice: eventTicketPrice,
            timeUntilEventStart: TimeSpan.FromDays(5),
            currentTime: CurrentTime);
    }

    [Fact]
    public void Calculate_WithConfirmedAndPendingReservations_ComputesRevenueAndOccupancyFromConfirmedOnly()
    {
        var targetEvent = CreateEvent(startDateTime: CurrentTime.AddDays(5), endDateTime: CurrentTime.AddDays(5).AddHours(3));

        var confirmed = CreateReservation(eventId: targetEvent.Id, quantity: 25, eventTicketPrice: 50m);
        confirmed.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);

        var pending = CreateReservation(eventId: targetEvent.Id, quantity: 5, eventTicketPrice: 50m);

        var report = OccupancyReportCalculator.Calculate(
            targetEvent: targetEvent,
            reservations: [confirmed, pending],
            currentTime: CurrentTime);

        Assert.Equal(expected: 25, actual: report.TotalSoldTickets);
        Assert.Equal(expected: 70, actual: report.TotalAvailableTickets);
        Assert.Equal(expected: 25d, actual: report.OccupancyPercentage);
        Assert.Equal(expected: 1250m, actual: report.TotalRevenue);
        Assert.Equal(expected: EventStatus.Active, actual: report.EventStatus);
    }

    [Fact]
    public void Calculate_AfterEventEndDate_ReportsCompletedStatus()
    {
        var targetEvent = CreateEvent(startDateTime: CurrentTime.AddHours(2), endDateTime: CurrentTime.AddHours(4));

        var report = OccupancyReportCalculator.Calculate(
            targetEvent: targetEvent,
            reservations: [],
            currentTime: CurrentTime.AddHours(5));

        Assert.Equal(expected: EventStatus.Completed, actual: report.EventStatus);
        Assert.Equal(expected: 0m, actual: report.TotalRevenue);
    }
}
