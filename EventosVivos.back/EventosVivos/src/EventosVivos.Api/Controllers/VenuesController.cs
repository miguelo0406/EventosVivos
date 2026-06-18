using EventosVivos.Api.Common;
using EventosVivos.Application.Commands.Venues;
using EventosVivos.Application.Dtos;
using EventosVivos.Application.Queries.Venues;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

// Adaptador de entrada del recurso /venues. La lectura la usa el asistente (dropdown del
// formulario de reserva/creación, RF-01); el CRUD es de organizador (valor agregado) y va
// protegido por la policy 'Organizer'.
[ApiController]
[Route("api/venues")]
public sealed class VenuesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VenuesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VenueResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VenueResponse>>>> ListVenues(
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(new ListVenuesQuery(), cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<VenueResponse>>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    [HttpPost]
    [Authorize(Policy = "Organizer")]
    [ProducesResponseType(typeof(ApiResponse<VenueResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VenueResponse>>> CreateVenue(
        [FromBody] CreateVenueRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new CreateVenueCommand(Request: request),
            cancellationToken
        );

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<VenueResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Organizer")]
    [ProducesResponseType(typeof(ApiResponse<VenueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<VenueResponse>>> UpdateVenue(
        int id,
        [FromBody] UpdateVenueRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new UpdateVenueCommand(VenueId: id, Request: request),
            cancellationToken
        );

        return Ok(
            ApiResponse<VenueResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteVenue(
        int id,
        CancellationToken cancellationToken
    )
    {
        await _mediator.Send(
            new DeleteVenueCommand(VenueId: id),
            cancellationToken
        );

        return NoContent();
    }
}
