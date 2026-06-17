using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Tests;

public sealed class TicketAvailabilityCalculatorTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid EventId = Guid.NewGuid();

    private static Reservation CreateReservation(int quantity)
    {
        return Reservation.Create(
            eventId: EventId,
            quantity: quantity,
            buyerName: "Comprador de prueba",
            buyerEmail: "comprador@example.com",
            availableTickets: 1000,
            eventTicketPrice: 50m,
            timeUntilEventStart: TimeSpan.FromDays(5),
            currentTime: CurrentTime);
    }

    [Fact]
    public void GetAvailableTickets_WithNoReservations_ReturnsFullCapacity()
    {
        var available = TicketAvailabilityCalculator.GetAvailableTickets(maxCapacity: 100, existingReservations: []);

        Assert.Equal(expected: 100, actual: available);
    }

    [Fact]
    public void GetAvailableTickets_WithPendingAndConfirmedReservations_SubtractsBoth()
    {
        var pending = CreateReservation(quantity: 10);
        var confirmed = CreateReservation(quantity: 20);
        confirmed.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);

        var available = TicketAvailabilityCalculator.GetAvailableTickets(
            maxCapacity: 100,
            existingReservations: [pending, confirmed]);

        Assert.Equal(expected: 70, actual: available);
    }

    [Fact]
    public void GetAvailableTickets_WithCancelledReservation_DoesNotSubtract()
    {
        var cancelled = CreateReservation(quantity: 30);
        cancelled.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromDays(5));

        var available = TicketAvailabilityCalculator.GetAvailableTickets(
            maxCapacity: 100,
            existingReservations: [cancelled]);

        Assert.Equal(expected: 100, actual: available);
    }

    // RN-07: una reserva "perdida" sigue ocupando cupo, no se libera para venta.
    [Fact]
    public void GetAvailableTickets_WithLostReservation_StillSubtracts()
    {
        var lost = CreateReservation(quantity: 15);
        lost.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);
        lost.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromHours(10));

        var available = TicketAvailabilityCalculator.GetAvailableTickets(
            maxCapacity: 100,
            existingReservations: [lost]);

        Assert.Equal(expected: 85, actual: available);
    }

    [Fact]
    public void GetAvailableTickets_NeverReturnsNegativeValue()
    {
        var overbooked = CreateReservation(quantity: 150);

        var available = TicketAvailabilityCalculator.GetAvailableTickets(
            maxCapacity: 100,
            existingReservations: [overbooked]);

        Assert.Equal(expected: 0, actual: available);
    }
}
