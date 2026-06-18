using EventosVivos.Api.Common;
using EventosVivos.Application.Commands.Auth;
using EventosVivos.Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

// Endpoints públicos de autenticación. [AllowAnonymous] porque son la puerta de entrada:
// aquí se obtiene el JWT que el resto de la API exige. Controller delgado → MediatR.
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new RegisterUserCommand(Request: request),
            cancellationToken
        );

        return Ok(
            ApiResponse<AuthResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new LoginCommand(Request: request),
            cancellationToken
        );

        return Ok(
            ApiResponse<AuthResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new RefreshTokenCommand(Request: request),
            cancellationToken
        );

        return Ok(
            ApiResponse<AuthResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }
}
