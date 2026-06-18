using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Auth;

// CQRS — Command: renueva el access token usando el refresh token.
public sealed record RefreshTokenCommand(
    RefreshTokenRequest Request
) : IRequest<AuthResponse>;