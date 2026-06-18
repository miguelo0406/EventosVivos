using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Auth;

internal sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    private readonly IAuthService _authService;

    public RegisterUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<AuthResponse> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken
    ) =>
        _authService.RegisterAsync(
            request: request.Request,
            cancellationToken: cancellationToken
        );
}
