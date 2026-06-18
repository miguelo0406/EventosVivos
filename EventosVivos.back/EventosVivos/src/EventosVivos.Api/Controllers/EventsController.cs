using EventosVivos.Api.Common;
using EventosVivos.Application.Commands.Events;
using EventosVivos.Application.Dtos;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Queries.Events;
using EventosVivos.Application.Queries.Reservations;
using EventosVivos.Domain.Ports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

// Adaptador de entrada (driving adapter). Controller delgado: traduce HTTP a un
// Command/Query de MediatR y envuelve la respuesta en ApiResponse. No conoce la lógica
// de negocio ni los servicios concretos, solo IMediator (mismo patrón que Contabot:
// controller pequeño → handler → service/facade).
[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // RF-01.
    [HttpPost]
    [Authorize(Policy = "Organizer")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new CreateEventCommand(Request: request),
            cancellationToken
        );

        return CreatedAtAction(
            actionName: nameof(GetEventById),
            routeValues: new { id = response.Id },
            value: ApiResponse<EventResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    // RF-02.
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EventResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EventResponse>>>> ListEvents(
        [FromQuery] string? type,
        [FromQuery] DateTime? fromStartDate,
        [FromQuery] DateTime? toStartDate,
        [FromQuery] int? venueId,
        [FromQuery] string? status,
        [FromQuery] string? title,
        CancellationToken cancellationToken
    )
    {
        var filter = new EventSearchFilter
        {
            Type = type is null ? null : EventTypeMapper.FromWireFormat(value: type),
            FromStartDate = fromStartDate,
            ToStartDate = toStartDate,
            VenueId = venueId,
            Status = status is null ? null : EventStatusMapper.FromWireFormat(value: status),
            TitleSearch = title,
        };

        var response = await _mediator.Send(
            new ListEventsQuery(Filter: filter),
            cancellationToken
        );

        return Ok(
            ApiResponse<IReadOnlyList<EventResponse>>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> GetEventById(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new GetEventByIdQuery(EventId: id),
            cancellationToken
        );

        return Ok(
            ApiResponse<EventResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    // Soporte: el estado "cancelado" es parte del contrato (RF-02, RF-06) y debe ser
    // alcanzable, aunque el enunciado no defina un RF explícito para cancelar eventos.
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEvent(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        await _mediator.Send(
            new CancelEventCommand(EventId: id),
            cancellationToken
        );

        return NoContent();
    }

    // RF-06.
    [HttpGet("{id:guid}/occupancy-report")]
    [Authorize(Policy = "Organizer")]
    [ProducesResponseType(typeof(ApiResponse<OccupancyReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OccupancyReportResponse>>> GetOccupancyReport(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new GetOccupancyReportQuery(EventId: id),
            cancellationToken
        );

        return Ok(
            ApiResponse<OccupancyReportResponse>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }

    // Soporte: permite a un administrador ver las reservas de un evento para decidir
    // cuáles confirmar (RF-04) o consultar antes de cancelarlas (RF-05).
    [HttpGet("{id:guid}/reservations")]
    [Authorize(Policy = "Organizer")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReservationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReservationResponse>>>> ListReservationsForEvent(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var response = await _mediator.Send(
            new ListReservationsByEventQuery(EventId: id),
            cancellationToken
        );

        return Ok(
            ApiResponse<IReadOnlyList<ReservationResponse>>.Success(
                data: response,
                requestId: HttpContext.TraceIdentifier
            )
        );
    }
}
