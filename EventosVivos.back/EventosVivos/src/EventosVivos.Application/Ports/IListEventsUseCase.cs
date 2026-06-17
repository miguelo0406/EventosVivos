using EventosVivos.Application.Dtos;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Ports;

// Puerto de entrada para RF-02.
public interface IListEventsUseCase
{
    Task<IReadOnlyList<EventResponse>> ExecuteAsync(EventSearchFilter filter, CancellationToken cancellationToken);
}
