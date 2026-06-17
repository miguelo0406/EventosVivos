using EventosVivos.Domain.Entities;

namespace EventosVivos.Domain.Ports;

// Puerto de salida (driven port): Application depende únicamente de esta abstracción,
// nunca de Entity Framework. Infrastructure la implementa.
// Inversión de dependencias (D de SOLID): el núcleo del negocio define el contrato;
// el detalle técnico (la base de datos) se adapta a él, y no al revés.
public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Event>> GetActiveEventsByVenueAsync(int venueId, CancellationToken cancellationToken);

    // Recibe currentTime porque el filtro por estado "completado" (RN-06) es derivado:
    // se traduce a la condición SQL (Status == Active && EndDateTime < currentTime).
    Task<IReadOnlyList<Event>> SearchAsync(EventSearchFilter filter, DateTime currentTime, CancellationToken cancellationToken);

    Task AddAsync(Event eventToAdd, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
