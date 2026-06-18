using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Services.Interfaces;

// Facade/servicio de aplicación de autenticación. Orquesta el IdP (Keycloak) y el
// espejo local de usuarios (provisioning JIT). Los handlers de MediatR delegan aquí.
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    );

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    );

    Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken
    );
}