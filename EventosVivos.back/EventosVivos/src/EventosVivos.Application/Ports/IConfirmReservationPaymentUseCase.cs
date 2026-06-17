using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada para RF-04.
public interface IConfirmReservationPaymentUseCase
{
    Task<ReservationResponse> ExecuteAsync(Guid reservationId, CancellationToken cancellationToken);
}
