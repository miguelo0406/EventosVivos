using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada de soporte: detalle de un evento (requerido por el frontend y por
// los flujos de reserva/reporte).
public interface IGetEventByIdUseCase
{
    Task<EventResponse> ExecuteAsync(Guid eventId, CancellationToken cancellationToken);
}
