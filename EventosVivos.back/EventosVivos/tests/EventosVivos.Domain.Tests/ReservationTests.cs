using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Tests;

public sealed class ReservationTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Reservation CreateValidReservation(
        int quantity = 2,
        int availableTickets = 50,
        decimal eventTicketPrice = 50m,
        TimeSpan? timeUntilEventStart = null)
    {
        return Reservation.Create(
            eventId: Guid.NewGuid(),
            quantity: quantity,
            buyerName: "Juan Pérez",
            buyerEmail: "juan.perez@example.com",
            availableTickets: availableTickets,
            eventTicketPrice: eventTicketPrice,
            timeUntilEventStart: timeUntilEventStart ?? TimeSpan.FromDays(5),
            currentTime: CurrentTime);
    }

    [Fact]
    public void Create_WithValidData_ReturnsPendingPaymentReservation()
    {
        var reservation = CreateValidReservation();

        Assert.Equal(expected: ReservationStatus.PendingPayment, actual: reservation.Status);
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsDomainValidationException()
    {
        var exception = Assert.Throws<DomainValidationException>(testCode: () => Reservation.Create(
            eventId: Guid.NewGuid(),
            quantity: 1,
            buyerName: "Juan Pérez",
            buyerEmail: "correo-invalido",
            availableTickets: 10,
            eventTicketPrice: 50m,
            timeUntilEventStart: TimeSpan.FromDays(5),
            currentTime: CurrentTime));

        Assert.Contains(collection: exception.Errors, filter: error => error.Contains("email"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithQuantityLessThanOne_ThrowsDomainValidationException(int invalidQuantity)
    {
        Assert.Throws<DomainValidationException>(testCode: () => CreateValidReservation(quantity: invalidQuantity));
    }

    // RN-04
    [Fact]
    public void Create_WithLessThanOneHourUntilStart_ThrowsLateReservationException()
    {
        Assert.Throws<LateReservationException>(testCode: () =>
            CreateValidReservation(timeUntilEventStart: TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void Create_WithExactlyOneHourUntilStart_DoesNotThrow()
    {
        var reservation = CreateValidReservation(timeUntilEventStart: TimeSpan.FromHours(1));

        Assert.Equal(expected: ReservationStatus.PendingPayment, actual: reservation.Status);
    }

    // RF-03: regla de las 24 horas
    [Fact]
    public void Create_WithLessThan24HoursUntilStartAndMoreThanFiveTickets_ThrowsTicketLimitExceededException()
    {
        Assert.Throws<TicketLimitExceededException>(testCode: () => CreateValidReservation(
            quantity: 6,
            timeUntilEventStart: TimeSpan.FromHours(10),
            eventTicketPrice: 20m));
    }

    [Fact]
    public void Create_WithLessThan24HoursUntilStartAndFiveTickets_DoesNotThrow()
    {
        var reservation = CreateValidReservation(
            quantity: 5,
            timeUntilEventStart: TimeSpan.FromHours(10),
            eventTicketPrice: 20m);

        Assert.Equal(expected: 5, actual: reservation.Quantity);
    }

    // RN-05: precio > $100
    [Fact]
    public void Create_WithPriceAboveOneHundredAndMoreThanTenTickets_ThrowsTicketLimitExceededException()
    {
        Assert.Throws<TicketLimitExceededException>(testCode: () => CreateValidReservation(
            quantity: 11,
            eventTicketPrice: 150m,
            timeUntilEventStart: TimeSpan.FromDays(5)));
    }

    [Fact]
    public void Create_WithPriceAboveOneHundredAndLessThan24Hours_AppliesMostRestrictiveLimit()
    {
        // RN-05 permitiría hasta 10, pero la regla de 24h (RF-03) exige máximo 5:
        // debe aplicar la más restrictiva entre ambas.
        Assert.Throws<TicketLimitExceededException>(testCode: () => CreateValidReservation(
            quantity: 6,
            eventTicketPrice: 150m,
            timeUntilEventStart: TimeSpan.FromHours(10)));
    }

    [Fact]
    public void Create_WithQuantityExceedingAvailableTickets_ThrowsInsufficientTicketsAvailableException()
    {
        Assert.Throws<InsufficientTicketsAvailableException>(testCode: () => CreateValidReservation(
            quantity: 10,
            availableTickets: 5));
    }

    [Fact]
    public void ConfirmPayment_WhenPending_SetsConfirmedStatusAndCode()
    {
        var reservation = CreateValidReservation();
        var code = ConfirmationCode.Generate();

        reservation.ConfirmPayment(confirmationCode: code, currentTime: CurrentTime);

        Assert.Equal(expected: ReservationStatus.Confirmed, actual: reservation.Status);
        Assert.Equal(expected: code, actual: reservation.ConfirmationCode);
        Assert.Equal(expected: CurrentTime, actual: reservation.ConfirmedAt);
    }

    [Fact]
    public void ConfirmPayment_WhenAlreadyConfirmed_ThrowsInvalidReservationStateTransitionException()
    {
        var reservation = CreateValidReservation();
        reservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);

        Assert.Throws<InvalidReservationStateTransitionException>(testCode: () => reservation.ConfirmPayment(
            confirmationCode: ConfirmationCode.Generate(),
            currentTime: CurrentTime));
    }

    [Fact]
    public void ConfirmPayment_WhenCancelled_ThrowsInvalidReservationStateTransitionException()
    {
        var reservation = CreateValidReservation();
        reservation.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromDays(5));

        Assert.Throws<InvalidReservationStateTransitionException>(testCode: () => reservation.ConfirmPayment(
            confirmationCode: ConfirmationCode.Generate(),
            currentTime: CurrentTime));
    }

    [Fact]
    public void Cancel_WhenPendingPayment_SetsCancelledStatus()
    {
        var reservation = CreateValidReservation();

        reservation.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromDays(5));

        Assert.Equal(expected: ReservationStatus.Cancelled, actual: reservation.Status);
        Assert.Equal(expected: CurrentTime, actual: reservation.CancelledAt);
    }

    // RN-07
    [Fact]
    public void Cancel_WhenConfirmedWithLessThan48HoursUntilEvent_SetsLostStatus()
    {
        var reservation = CreateValidReservation();
        reservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);

        reservation.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromHours(40));

        Assert.Equal(expected: ReservationStatus.Lost, actual: reservation.Status);
    }

    [Fact]
    public void Cancel_WhenConfirmedWithMoreThan48HoursUntilEvent_SetsCancelledStatus()
    {
        var reservation = CreateValidReservation();
        reservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);

        reservation.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromHours(72));

        Assert.Equal(expected: ReservationStatus.Cancelled, actual: reservation.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsInvalidReservationStateTransitionException()
    {
        var reservation = CreateValidReservation();
        reservation.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromDays(5));

        Assert.Throws<InvalidReservationStateTransitionException>(testCode: () => reservation.Cancel(
            currentTime: CurrentTime,
            timeUntilEventStart: TimeSpan.FromDays(5)));
    }

    [Fact]
    public void Cancel_WhenAlreadyLost_ThrowsInvalidReservationStateTransitionException()
    {
        var reservation = CreateValidReservation();
        reservation.ConfirmPayment(confirmationCode: ConfirmationCode.Generate(), currentTime: CurrentTime);
        reservation.Cancel(currentTime: CurrentTime, timeUntilEventStart: TimeSpan.FromHours(10));

        Assert.Throws<InvalidReservationStateTransitionException>(testCode: () => reservation.Cancel(
            currentTime: CurrentTime,
            timeUntilEventStart: TimeSpan.FromHours(10)));
    }
}
