using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Queries.Events;

// CQRS — Query.
public sealed record GetEventByIdQuery(
    Guid EventId
) : IRequest<EventResponse>;