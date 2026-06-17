using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada para RF-03.
public interface IReserveTicketsUseCase
{
    Task<ReservationResponse> ExecuteAsync(ReserveTicketsRequest request, CancellationToken cancellationToken);
}
