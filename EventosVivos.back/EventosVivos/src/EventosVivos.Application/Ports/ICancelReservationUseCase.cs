using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada para RF-05.
public interface ICancelReservationUseCase
{
    Task<ReservationResponse> ExecuteAsync(Guid reservationId, CancellationToken cancellationToken);
}
