using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada de soporte: necesario para que el frontend liste los venues
// preexistentes al crear un evento (RF-01: "Venue, referencia a un lugar
// preexistente").
public interface IListVenuesUseCase
{
    Task<IReadOnlyList<VenueResponse>> ExecuteAsync(CancellationToken cancellationToken);
}
