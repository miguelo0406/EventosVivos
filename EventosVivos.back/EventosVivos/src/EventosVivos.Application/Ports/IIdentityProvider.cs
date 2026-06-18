using EventosVivos.Application.Models;

namespace EventosVivos.Application.Ports;

// Puerto de salida hacia el proveedor de identidad (Keycloak). Application orquesta la
// autenticación contra esta abstracción; la implementación concreta (HTTP a Keycloak)
// vive en Infrastructure. Inversión de dependencias (D de SOLID): cambiar Keycloak por
// otro IdP (Auth0, Azure AD B2C) sería un nuevo adaptador, sin tocar Application.
public interface IIdentityProvider
{
    // Crea el usuario en el IdP y devuelve su identificador (subject / claim `sub`).
    Task<string> RegisterAsync(
        string email,
        string password,
        string? fullName,
        CancellationToken cancellationToken
    );

    // Login por credenciales (direct grant). Devuelve los tokens y la identidad decodificada.
    Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken
    );

    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken
    );
}