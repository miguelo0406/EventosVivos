using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Ports;

// Puerto de entrada para RF-06.
public interface IGetOccupancyReportUseCase
{
    Task<OccupancyReportResponse> ExecuteAsync(Guid eventId, CancellationToken cancellationToken);
}
