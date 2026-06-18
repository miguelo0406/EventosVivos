using EventosVivos.Application.Dtos;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Services.Interfaces;

// Facade/servicio de aplicación del agregado Event. Es el "motor de negocio" del
// módulo: los handlers de MediatR (Commands/Queries) son delgados y delegan aquí toda
// la orquestación. Inversión de dependencias (D de SOLID): depende solo de puertos de
// Domain, nunca de EF Core ni de HTTP.
public interface IEventService
{
    Task<EventResponse> CreateAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<EventResponse>> SearchAsync(
        EventSearchFilter filter,
        CancellationToken cancellationToken
    );

    Task<EventResponse> GetByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken
    );

    Task CancelAsync(
        Guid eventId,
        CancellationToken cancellationToken
    );

    Task<OccupancyReportResponse> GetOccupancyReportAsync(
        Guid eventId,
        CancellationToken cancellationToken
    );
}