namespace EventosVivos.Application.Dtos;

// Lo que el frontend necesita tras login/registro/refresh. El access token es de vida
// corta (5 min); cuando esté por expirar, el front usa el refresh token para renovarlo
// mientras haya actividad. Tras 5 min de inactividad el refresh falla (idle de Keycloak).
public sealed record AuthResponse
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required int ExpiresIn { get; init; }

    public required int RefreshExpiresIn { get; init; }

    public required string TokenType { get; init; }

    public required string Email { get; init; }
}
