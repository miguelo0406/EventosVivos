using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada (driving port) para RF-01. La capa Api depende de esta
// abstracción, nunca de la implementación concreta: Dependency Inversion (D de
// SOLID) aplicado también en el borde de entrada, no solo hacia la persistencia.
public interface ICreateEventUseCase
{
    Task<EventResponse> ExecuteAsync(CreateEventRequest request, CancellationToken cancellationToken);
}
