using EventosVivos.Domain.Entities;

namespace EventosVivos.Domain.Ports;

// Puerto de salida. Interface Segregation (I de SOLID): se mantiene separado de
// IEventRepository e IReservationRepository para que cada caso de uso dependa solo
// del contrato que realmente necesita, no de una interfaz "god repository".
public interface IVenueRepository
{
    Task<Venue?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Venue>> GetAllAsync(
        CancellationToken cancellationToken
    );

    // El Id de venue es ValueGeneratedNever (la tabla conserva el seed fijo 1-3); el servicio
    // asigna el siguiente como max(Id)+1 al crear, evitando una migración a columna identity.
    Task<int> GetMaxIdAsync(
        CancellationToken cancellationToken
    );

    Task AddAsync(
        Venue venue,
        CancellationToken cancellationToken
    );

    void Remove(Venue venue);

    Task SaveChangesAsync(
        CancellationToken cancellationToken
    );
}