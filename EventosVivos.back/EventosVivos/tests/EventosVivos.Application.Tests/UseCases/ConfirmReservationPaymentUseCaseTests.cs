using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Tests.TestDoubles;
using EventosVivos.Application.UseCases;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Tests.UseCases;

public sealed class ConfirmReservationPaymentUseCaseTests
{
    private static readonly DateTime CurrentTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private static async Task<(ConfirmReservationPaymentUseCase UseCase, Reservation Reservation)> CreateSutAsync()
    {
        var reservations = new InMemoryReservationRepository();
        var reservation = Reservation.Create(
            eventId: Guid.NewGuid(),
            quantity: 2,
            buyerName: "Comprador de prueba",
            buyerEmail: "comprador@example.com",
            availableTickets: 100,
            eventTicketPrice: 50m,
            timeUntilEventStart: TimeSpan.FromDays(10),
            currentTime: CurrentTime);
        await reservations.AddAsync(reservation: reservation, cancellationToken: CancellationToken.None);

        var useCase = new ConfirmReservationPaymentUseCase(
            reservationRepository: reservations,
            clock: new FixedClock(utcNow: CurrentTime));

        return (useCase, reservation);
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingReservation_GeneratesConfirmationCode()
    {
        var (useCase, reservation) = await CreateSutAsync();

        var response = await useCase.ExecuteAsync(reservationId: reservation.Id, cancellationToken: CancellationToken.None);

        Assert.Equal(expected: "confirmada", actual: response.Status);
        Assert.NotNull(response.ConfirmationCode);
        Assert.Matches(expectedRegexPattern: "^EV-\\d{6}$", actualString: response.ConfirmationCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyConfirmed_ThrowsInvalidReservationStateTransitionException()
    {
        var (useCase, reservation) = await CreateSutAsync();
        await useCase.ExecuteAsync(reservationId: reservation.Id, cancellationToken: CancellationToken.None);

        await Assert.ThrowsAsync<InvalidReservationStateTransitionException>(testCode: () => useCase.ExecuteAsync(
            reservationId: reservation.Id,
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownReservation_ThrowsReservationNotFoundException()
    {
        var (useCase, _) = await CreateSutAsync();

        await Assert.ThrowsAsync<ReservationNotFoundException>(testCode: () => useCase.ExecuteAsync(
            reservationId: Guid.NewGuid(),
            cancellationToken: CancellationToken.None));
    }
}
