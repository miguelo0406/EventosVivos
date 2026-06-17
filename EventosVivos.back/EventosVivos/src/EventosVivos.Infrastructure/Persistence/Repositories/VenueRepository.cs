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

    public async Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Venues
            .FirstOrDefaultAsync(venue => venue.Id == id, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Venues
            .OrderBy(venue => venue.Id)
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
