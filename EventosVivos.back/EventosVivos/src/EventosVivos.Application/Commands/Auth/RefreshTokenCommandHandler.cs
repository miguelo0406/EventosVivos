using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Auth;

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<AuthResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken
    ) =>
        _authService.RefreshAsync(
            request: request.Request,
            cancellationToken: cancellationToken
        );
}
