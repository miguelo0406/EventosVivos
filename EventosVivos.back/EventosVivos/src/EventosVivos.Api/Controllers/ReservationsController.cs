using EventosVivos.Api.Common;
using EventosVivos.Application.Commands.Reservations;
using EventosVivos.Application.Dtos;
using EventosVivos.Application.Queries.Reservations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // RF-03.
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> ReserveTickets(
        [FromBody] ReserveTicketsRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new ReserveTicketsCommand(Request: request),
            cancellationToken
        );

        return CreatedAtAction(
            actionName: nameof(GetReservationById),
            routeValues: new { id = response.Id },
            value: ApiResponse<ReservationResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> GetReservationById(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new GetReservationByIdQuery(ReservationId: id),
            cancellationToken
        );

        return Ok(
            ApiResponse<ReservationResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    // RF-04.
    [HttpPost("{id:guid}/confirm-payment")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> ConfirmPayment(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new ConfirmReservationPaymentCommand(ReservationId: id),
            cancellationToken
        );

        return Ok(
            ApiResponse<ReservationResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    // RF-05.
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> CancelReservation(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new CancelReservationCommand(ReservationId: id),
            cancellationToken
        );

        return Ok(
            ApiResponse<ReservationResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }
}
