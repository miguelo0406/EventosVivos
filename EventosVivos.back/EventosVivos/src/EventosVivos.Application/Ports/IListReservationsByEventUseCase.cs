using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada de soporte: RF-04 asume que el administrador puede ver las
// reservas de un evento para decidir cuáles confirmar; sin este listado no habría
// forma de descubrir los IDs de reserva a confirmar desde el frontend.
public interface IListReservationsByEventUseCase
{
    Task<IReadOnlyList<ReservationResponse>> ExecuteAsync(Guid eventId, CancellationToken cancellationToken);
}
