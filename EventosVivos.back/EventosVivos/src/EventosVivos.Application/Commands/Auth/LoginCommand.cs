using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Auth;

// CQRS — Command: autentica por credenciales y devuelve tokens (RF de login).
public sealed record LoginCommand(
    LoginRequest Request
) : IRequest<AuthResponse>;