using EventosVivos.Domain.Entities;

namespace EventosVivos.Domain.Ports;

// Puerto de salida para el espejo local de usuarios. Igual que el resto de puertos,
// Application depende de esta abstracción y nunca de EF Core (Inversión de
// dependencias, D de SOLID).
public interface IUserRepository
{
    Task<User?> GetByKeycloakSubjectIdAsync(
        string keycloakSubjectId,
        CancellationToken cancellationToken
    );

    Task AddAsync(
        User user,
        CancellationToken cancellationToken
    );

    Task SaveChangesAsync(
        CancellationToken cancellationToken
    );
}