using EventosVivos.Application.Dtos;
using EventosVivos.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

// Soporte: el frontend necesita listar los venues preexistentes para el formulario
// de creación de eventos (RF-01: "Venue, referencia a un lugar preexistente").
[ApiController]
[Route("api/venues")]
public sealed class VenuesController : ControllerBase
{
    private readonly IListVenuesUseCase _listVenuesUseCase;

    public VenuesController(IListVenuesUseCase listVenuesUseCase)
    {
        _listVenuesUseCase = listVenuesUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VenueResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VenueResponse>>> ListVenues(CancellationToken cancellationToken)
    {
        var response = await _listVenuesUseCase.ExecuteAsync(cancellationToken: cancellationToken);

        return Ok(response);
    }
}
