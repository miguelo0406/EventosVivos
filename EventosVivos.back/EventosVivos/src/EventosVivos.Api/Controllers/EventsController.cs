using EventosVivos.Application.Dtos;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

// Adaptador de entrada (driving adapter): traduce HTTP a llamadas contra los puertos
// de Application. Depende de interfaces (ICreateEventUseCase, etc.), nunca de las
// clases concretas: Inversión de dependencias (D de SOLID) en el borde de entrada.
[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly ICreateEventUseCase _createEventUseCase;
    private readonly IListEventsUseCase _listEventsUseCase;
    private readonly IGetEventByIdUseCase _getEventByIdUseCase;
    private readonly ICancelEventUseCase _cancelEventUseCase;
    private readonly IGetOccupancyReportUseCase _getOccupancyReportUseCase;
    private readonly IListReservationsByEventUseCase _listReservationsByEventUseCase;

    public EventsController(
        ICreateEventUseCase createEventUseCase,
        IListEventsUseCase listEventsUseCase,
        IGetEventByIdUseCase getEventByIdUseCase,
        ICancelEventUseCase cancelEventUseCase,
        IGetOccupancyReportUseCase getOccupancyReportUseCase,
        IListReservationsByEventUseCase listReservationsByEventUseCase)
    {
        _createEventUseCase = createEventUseCase;
        _listEventsUseCase = listEventsUseCase;
        _getEventByIdUseCase = getEventByIdUseCase;
        _cancelEventUseCase = cancelEventUseCase;
        _getOccupancyReportUseCase = getOccupancyReportUseCase;
        _listReservationsByEventUseCase = listReservationsByEventUseCase;
    }

    // RF-01.
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EventResponse>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _createEventUseCase.ExecuteAsync(request: request, cancellationToken: cancellationToken);

        return CreatedAtAction(
            actionName: nameof(GetEventById),
            routeValues: new { id = response.Id },
            value: response);
    }

    // RF-02.
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> ListEvents(
        [FromQuery] string? type,
        [FromQuery] DateTime? fromStartDate,
        [FromQuery] int? venueId,
        [FromQuery] string? status,
        [FromQuery] string? title,
        CancellationToken cancellationToken)
    {
        var filter = new EventSearchFilter
        {
            Type = type is null ? null : EventTypeMapper.FromWireFormat(value: type),
            FromStartDate = fromStartDate,
            VenueId = venueId,
            Status = status is null ? null : EventStatusMapper.FromWireFormat(value: status),
            TitleSearch = title,
        };

        var response = await _listEventsUseCase.ExecuteAsync(filter: filter, cancellationToken: cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEventById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _getEventByIdUseCase.ExecuteAsync(eventId: id, cancellationToken: cancellationToken);

        return Ok(response);
    }

    // Soporte: no hay un RF explícito para cancelar eventos, pero el estado
    // "cancelado" es parte del contrato (RF-02, RF-06) y debe ser alcanzable.
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEvent(Guid id, CancellationToken cancellationToken)
    {
        await _cancelEventUseCase.ExecuteAsync(eventId: id, cancellationToken: cancellationToken);

        return NoContent();
    }

    // RF-06.
    [HttpGet("{id:guid}/occupancy-report")]
    [ProducesResponseType(typeof(OccupancyReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OccupancyReportResponse>> GetOccupancyReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _getOccupancyReportUseCase.ExecuteAsync(eventId: id, cancellationToken: cancellationToken);

        return Ok(response);
    }

    // Soporte: permite a un administrador ver las reservas de un evento para decidir
    // cuáles confirmar (RF-04) o consultar antes de cancelarlas (RF-05).
    [HttpGet("{id:guid}/reservations")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> ListReservationsForEvent(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _listReservationsByEventUseCase.ExecuteAsync(eventId: id, cancellationToken: cancellationToken);

        return Ok(response);
    }
}
