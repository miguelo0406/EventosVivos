using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Tests.TestDoubles;

public sealed class InMemoryVenueRepository : IVenueRepository
{
    private readonly List<Venue> _venues;

    public InMemoryVenueRepository(IEnumerable<Venue>? seedVenues = null)
    {
        _venues = seedVenues?.ToList() ?? [];
    }

    public Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_venues.FirstOrDefault(venue => venue.Id == id));
    }

    public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Venue> result = _venues.ToList();
        return Task.FromResult(result);
    }
}
