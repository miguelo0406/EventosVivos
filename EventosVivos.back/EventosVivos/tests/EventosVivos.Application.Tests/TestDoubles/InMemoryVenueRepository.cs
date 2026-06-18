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

    public Task<int> GetMaxIdAsync(CancellationToken cancellationToken)
    {
        var maxId = _venues.Count == 0 ? 0 : _venues.Max(venue => venue.Id);
        return Task.FromResult(maxId);
    }

    public Task AddAsync(Venue venue, CancellationToken cancellationToken)
    {
        _venues.Add(venue);
        return Task.CompletedTask;
    }

    public void Remove(Venue venue)
    {
        _venues.Remove(venue);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
