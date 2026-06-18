using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Entities;

// Espejo local delgado del usuario que vive en Keycloak. Keycloak es la fuente de
// verdad de la identidad (credenciales, login); esta tabla solo guarda lo mínimo para
// relacionar los datos de dominio con la cuenta: el Id local y el "subject" de Keycloak
// (claim `sub` del JWT, un GUID inmutable). Email se guarda para mostrar/buscar sin
// llamar al Admin API de Keycloak en cada request.
public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public Email Email { get; private set; } = null!;

    // Claim `sub` del token de Keycloak: la columna de enlace entre ambas partes.
    public string KeycloakSubjectId { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static User Create(
        string email,
        string keycloakSubjectId,
        DateTime currentTime
    )
    {
        if (string.IsNullOrWhiteSpace(keycloakSubjectId))
        {
            throw new ArgumentException(message: "El subject de Keycloak es obligatorio.",
                paramName: nameof(keycloakSubjectId));
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Email = Email.Create(value: email),
            KeycloakSubjectId = keycloakSubjectId,
            CreatedAt = currentTime,
        };
    }
}