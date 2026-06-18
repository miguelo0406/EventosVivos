using EventosVivos.Application.Dtos;
using EventosVivos.Domain.Ports;
using MediatR;

namespace EventosVivos.Application.Queries.Events;

// CQRS — Query (RF-02): lectura con filtros. Separada de los Commands para dejar
// explícita la segregación lectura/escritura.
public sealed record ListEventsQuery(
    EventSearchFilter Filter
) : IRequest<IReadOnlyList<EventResponse>>;