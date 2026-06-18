using EventosVivos.Domain.Entities;

namespace EventosVivos.Domain.Ports;

// Puerto de salida. Ver IEventRepository para la justificación de Dependency
// Inversion aplicada en todos los puertos de Domain.
public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Reservation>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken
    );

    Task<bool> ExistsByConfirmationCodeAsync(
        string confirmationCode,
        CancellationToken cancellationToken
    );

    Task AddAsync(
        Reservation reservation,
        CancellationToken cancellationToken
    );

    Task SaveChangesAsync(CancellationToken cancellationToken);
}