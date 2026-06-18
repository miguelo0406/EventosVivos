using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Tests.TestDoubles;

// Doble de prueba que implementa IEventRepository sin base de datos real.
// Sustitución de Liskov (L de SOLID): los casos de uso funcionan igual con este
// repositorio en memoria que con EventRepository (EF Core), porque ambos cumplen el
// mismo contrato definido en Domain.
public sealed class InMemoryEventRepository : IEventRepository
{
    private readonly List<Event> _events = [];

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_events.FirstOrDefault(eventEntity => eventEntity.Id == id));
    }

    public Task<IReadOnlyList<Event>> GetActiveEventsByVenueAsync(int venueId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> result = _events
            .Where(eventEntity => eventEntity.VenueId == venueId && eventEntity.Status == EventStatus.Active)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> AnyByVenueAsync(int venueId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_events.Any(eventEntity => eventEntity.VenueId == venueId));
    }

    public Task<IReadOnlyList<Event>> SearchAsync(
        EventSearchFilter filter,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        IEnumerable<Event> query = _events;

        if (filter.Type.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.Type == filter.Type.Value);
        }

        if (filter.VenueId.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.VenueId == filter.VenueId.Value);
        }

        if (filter.FromStartDate.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.StartDateTime >= filter.FromStartDate.Value);
        }

        if (filter.ToStartDate.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.StartDateTime <= filter.ToStartDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TitleSearch))
        {
            query = query.Where(eventEntity =>
                eventEntity.Title.Contains(filter.TitleSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.GetEffectiveStatus(currentTime: currentTime) == filter.Status.Value);
        }

        IReadOnlyList<Event> result = query.ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(Event eventToAdd, CancellationToken cancellationToken)
    {
        _events.Add(eventToAdd);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
