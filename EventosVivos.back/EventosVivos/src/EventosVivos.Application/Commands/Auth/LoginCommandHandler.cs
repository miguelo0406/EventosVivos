using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Auth;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<AuthResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken
    ) =>
        _authService.LoginAsync(
            request: request.Request,
            cancellationToken: cancellationToken
        );
}
