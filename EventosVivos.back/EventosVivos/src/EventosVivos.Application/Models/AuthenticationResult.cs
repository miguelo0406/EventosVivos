namespace EventosVivos.Application.Models;

// Resultado interno de una autenticación contra el IdP: tokens + identidad decodificada
// del access token (subject y email), que Application usa para el provisioning JIT del
// usuario local.
public sealed record AuthenticationResult
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required int ExpiresIn { get; init; }

    public required int RefreshExpiresIn { get; init; }

    public required string TokenType { get; init; }

    public required string Subject { get; init; }

    public required string Email { get; init; }
}
