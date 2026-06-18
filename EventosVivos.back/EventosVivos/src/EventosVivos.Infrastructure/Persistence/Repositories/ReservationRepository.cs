using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Ports;
using EventosVivos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly EventosVivosDbContext _dbContext;

    public ReservationRepository(EventosVivosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Reservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Reservations
            .FirstOrDefaultAsync(
                reservation => reservation.Id == id,
                cancellationToken: cancellationToken
            );
    }

    public async Task<IReadOnlyList<Reservation>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Reservations
            .Where(reservation => reservation.EventId == eventId)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsByConfirmationCodeAsync(
        string confirmationCode,
        CancellationToken cancellationToken
    )
    {
        var candidateCode = ConfirmationCode.FromExisting(confirmationCode);

        return await _dbContext.Reservations
            .AnyAsync(reservation => reservation.ConfirmationCode == candidateCode,
                cancellationToken: cancellationToken);
    }

    public async Task AddAsync(
        Reservation reservation,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.Reservations.AddAsync(reservation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}