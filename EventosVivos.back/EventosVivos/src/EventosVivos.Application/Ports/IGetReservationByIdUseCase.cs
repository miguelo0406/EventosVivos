using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada de soporte: necesario para poder enlazar CreatedAtAction tras
// RF-03 y para que el frontend pueda refrescar el estado de una reserva puntual.
public interface IGetReservationByIdUseCase
{
    Task<ReservationResponse> ExecuteAsync(Guid reservationId, CancellationToken cancellationToken);
}
