using EventosVivos.Application.Dtos;
using EventosVivos.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReserveTicketsUseCase _reserveTicketsUseCase;
    private readonly IGetReservationByIdUseCase _getReservationByIdUseCase;
    private readonly IConfirmReservationPaymentUseCase _confirmReservationPaymentUseCase;
    private readonly ICancelReservationUseCase _cancelReservationUseCase;

    public ReservationsController(
        IReserveTicketsUseCase reserveTicketsUseCase,
        IGetReservationByIdUseCase getReservationByIdUseCase,
        IConfirmReservationPaymentUseCase confirmReservationPaymentUseCase,
        ICancelReservationUseCase cancelReservationUseCase)
    {
        _reserveTicketsUseCase = reserveTicketsUseCase;
        _getReservationByIdUseCase = getReservationByIdUseCase;
        _confirmReservationPaymentUseCase = confirmReservationPaymentUseCase;
        _cancelReservationUseCase = cancelReservationUseCase;
    }

    // RF-03.
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> ReserveTickets(
        [FromBody] ReserveTicketsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _reserveTicketsUseCase.ExecuteAsync(request: request, cancellationToken: cancellationToken);

        return CreatedAtAction(
            actionName: nameof(GetReservationById),
            routeValues: new { id = response.Id },
            value: response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetReservationById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _getReservationByIdUseCase.ExecuteAsync(reservationId: id, cancellationToken: cancellationToken);

        return Ok(response);
    }

    // RF-04.
    [HttpPost("{id:guid}/confirm-payment")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> ConfirmPayment(Guid id, CancellationToken cancellationToken)
    {
        var response = await _confirmReservationPaymentUseCase.ExecuteAsync(
            reservationId: id,
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    // RF-05.
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> CancelReservation(Guid id, CancellationToken cancellationToken)
    {
        var response = await _cancelReservationUseCase.ExecuteAsync(reservationId: id, cancellationToken: cancellationToken);

        return Ok(response);
    }
}
