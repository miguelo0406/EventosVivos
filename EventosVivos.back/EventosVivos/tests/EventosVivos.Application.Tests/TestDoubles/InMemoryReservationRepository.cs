using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Tests.TestDoubles;

public sealed class InMemoryReservationRepository : IReservationRepository
{
    private readonly List<Reservation> _reservations = [];

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_reservations.FirstOrDefault(reservation => reservation.Id == id));
    }

    public Task<IReadOnlyList<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Reservation> result = _reservations
            .Where(reservation => reservation.EventId == eventId)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> ExistsByConfirmationCodeAsync(string confirmationCode, CancellationToken cancellationToken)
    {
        var exists = _reservations.Any(reservation => reservation.ConfirmationCode?.Value == confirmationCode);
        return Task.FromResult(exists);
    }

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        _reservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
