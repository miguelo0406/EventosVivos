using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Auth;

// CQRS — Command: registra un usuario en Keycloak + espejo local y devuelve tokens.
public sealed record RegisterUserCommand(
    RegisterRequest Request
) : IRequest<AuthResponse>;