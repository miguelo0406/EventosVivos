using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence.Repositories;

public sealed class VenueRepository : IVenueRepository
{
    private readonly EventosVivosDbContext _dbContext;

    public VenueRepository(EventosVivosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Venue?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Venues
            .FirstOrDefaultAsync(venue => venue.Id == id, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Venue>> GetAllAsync(
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Venues
            .OrderBy(venue => venue.Id)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<int> GetMaxIdAsync(
        CancellationToken cancellationToken
    )
    {
        // Si no hay venues devuelve 0; el servicio usará max+1 como nuevo Id. Se proyecta a int?
        // para que Npgsql traduzca el MAX y devuelva NULL en tabla vacía (DefaultIfEmpty no es traducible).
        var maxId = await _dbContext.Venues
            .MaxAsync(venue => (int?)venue.Id, cancellationToken: cancellationToken);
        return maxId ?? 0;
    }

    public async Task AddAsync(
        Venue venue,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.Venues.AddAsync(venue, cancellationToken);
    }

    public void Remove(
        Venue venue
    )
    {
        _dbContext.Venues.Remove(venue);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken
    )
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}