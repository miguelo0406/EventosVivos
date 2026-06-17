using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence.Repositories;

// Adaptador de salida: implementa el puerto IEventRepository definido en Domain.
// Sustitución de Liskov (L de SOLID): cualquier caso de uso que dependa de
// IEventRepository funciona igual con esta implementación EF Core o, en pruebas, con
// un repositorio en memoria.
public sealed class EventRepository : IEventRepository
{
    private readonly EventosVivosDbContext _dbContext;

    public EventRepository(EventosVivosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Events
            .FirstOrDefaultAsync(eventEntity => eventEntity.Id == id, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetActiveEventsByVenueAsync(int venueId, CancellationToken cancellationToken)
    {
        return await _dbContext.Events
            .Where(eventEntity => eventEntity.VenueId == venueId && eventEntity.Status == EventStatus.Active)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> SearchAsync(
        EventSearchFilter filter,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Events.AsQueryable();

        if (filter.Type.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.Type == filter.Type.Value);
        }

        if (filter.FromStartDate.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.StartDateTime >= filter.FromStartDate.Value);
        }

        if (filter.VenueId.HasValue)
        {
            query = query.Where(eventEntity => eventEntity.VenueId == filter.VenueId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TitleSearch))
        {
            query = query.Where(eventEntity => EF.Functions.ILike(eventEntity.Title, $"%{filter.TitleSearch}%"));
        }

        if (filter.Status.HasValue)
        {
            // RN-06: "completado" es un estado derivado, se traduce a la condición
            // equivalente sobre las columnas persistidas (Status + EndDateTime).
            query = filter.Status.Value switch
            {
                EventStatus.Cancelled => query.Where(eventEntity => eventEntity.Status == EventStatus.Cancelled),
                EventStatus.Completed => query.Where(eventEntity =>
                    eventEntity.Status == EventStatus.Active && eventEntity.EndDateTime < currentTime),
                EventStatus.Active => query.Where(eventEntity =>
                    eventEntity.Status == EventStatus.Active && eventEntity.EndDateTime >= currentTime),
                _ => query,
            };
        }

        return await query
            .OrderBy(eventEntity => eventEntity.StartDateTime)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task AddAsync(Event eventToAdd, CancellationToken cancellationToken)
    {
        await _dbContext.Events.AddAsync(eventToAdd, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
