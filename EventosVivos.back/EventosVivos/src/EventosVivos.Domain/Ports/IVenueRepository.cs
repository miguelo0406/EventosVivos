using EventosVivos.Domain.Entities;

namespace EventosVivos.Domain.Ports;

// Puerto de salida. Interface Segregation (I de SOLID): se mantiene separado de
// IEventRepository e IReservationRepository para que cada caso de uso dependa solo
// del contrato que realmente necesita, no de una interfaz "god repository".
public interface IVenueRepository
{
    Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken);
}
