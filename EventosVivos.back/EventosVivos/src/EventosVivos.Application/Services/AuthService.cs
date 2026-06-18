using EventosVivos.Application.Dtos;
using EventosVivos.Application.Models;
using EventosVivos.Application.Ports;
using EventosVivos.Application.Services.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Services;

// Responsabilidad única (S de SOLID): orquesta autenticación. Keycloak maneja las
// credenciales (vía IIdentityProvider); este servicio solo coordina el alta en el IdP,
// el espejo local del usuario (provisioning JIT) y arma el AuthResponse para el front.
public sealed class AuthService : IAuthService
{
    private readonly IIdentityProvider _identityProvider;
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;

    public AuthService(
        IIdentityProvider identityProvider,
        IUserRepository userRepository,
        IClock clock
    )
    {
        _identityProvider = identityProvider;
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        // 1) Crea el usuario en Keycloak (fuente de verdad de la identidad).
        var keycloakSubjectId = await _identityProvider.RegisterAsync(
            email: request.Email,
            password: request.Password,
            fullName: request.FullName,
            cancellationToken: cancellationToken
        );

        // 2) Crea el espejo local enlazado por el subject de Keycloak.
        await EnsureLocalUserAsync(
            keycloakSubjectId: keycloakSubjectId,
            email: request.Email,
            cancellationToken: cancellationToken
        );

        // 3) Auto-login: el front recibe los tokens y queda autenticado tras registrarse.
        var authentication = await _identityProvider.LoginAsync(
            email: request.Email,
            password: request.Password,
            cancellationToken: cancellationToken
        );

        return ToResponse(authentication: authentication);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var authentication = await _identityProvider.LoginAsync(
            email: request.Email,
            password: request.Password,
            cancellationToken: cancellationToken
        );

        // Provisioning JIT: si el usuario existe en Keycloak pero aún no tiene espejo
        // local (p. ej. creado directamente en Keycloak), se crea en el primer login.
        await EnsureLocalUserAsync(
            keycloakSubjectId: authentication.Subject,
            email: authentication.Email,
            cancellationToken: cancellationToken
        );

        return ToResponse(authentication: authentication);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken
    )
    {
        var authentication = await _identityProvider.RefreshAsync(
            refreshToken: request.RefreshToken,
            cancellationToken: cancellationToken
        );

        return ToResponse(authentication: authentication);
    }

    private async Task EnsureLocalUserAsync(
        string keycloakSubjectId,
        string email,
        CancellationToken cancellationToken
    )
    {
        var existing = await _userRepository.GetByKeycloakSubjectIdAsync(
            keycloakSubjectId: keycloakSubjectId,
            cancellationToken: cancellationToken
        );

        if (existing is not null)
        {
            return;
        }

        var user = User.Create(
            email: email,
            keycloakSubjectId: keycloakSubjectId,
            currentTime: _clock.UtcNow
        );

        await _userRepository.AddAsync(
            user: user,
            cancellationToken: cancellationToken
        );
        await _userRepository.SaveChangesAsync(cancellationToken: cancellationToken);
    }

    private static AuthResponse ToResponse(AuthenticationResult authentication)
    {
        return new()
        {
            AccessToken = authentication.AccessToken,
            RefreshToken = authentication.RefreshToken,
            ExpiresIn = authentication.ExpiresIn,
            RefreshExpiresIn = authentication.RefreshExpiresIn,
            TokenType = authentication.TokenType,
            Email = authentication.Email,
        };
    }
}