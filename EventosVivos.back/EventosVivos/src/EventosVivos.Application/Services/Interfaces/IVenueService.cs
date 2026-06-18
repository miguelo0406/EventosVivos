using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Services.Interfaces;

// Facade/servicio de aplicación del agregado Venue. Lectura (datos de referencia) + un CRUD
// de organizador (valor agregado) que protege las invariantes cruzadas con Event.
public interface IVenueService
{
    Task<IReadOnlyList<VenueResponse>> GetAllAsync(
        CancellationToken cancellationToken
    );

    Task<VenueResponse> CreateAsync(
        CreateVenueRequest request,
        CancellationToken cancellationToken
    );

    Task<VenueResponse> UpdateAsync(
        int venueId,
        UpdateVenueRequest request,
        CancellationToken cancellationToken
    );

    Task DeleteAsync(
        int venueId,
        CancellationToken cancellationToken
    );
}