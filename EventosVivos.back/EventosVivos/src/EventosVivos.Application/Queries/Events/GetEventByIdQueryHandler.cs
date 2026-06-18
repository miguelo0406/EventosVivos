using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Queries.Events;

internal sealed class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventResponse>
{
    private readonly IEventService _eventService;

    public GetEventByIdQueryHandler(IEventService eventService)
    {
        _eventService = eventService;
    }

    public Task<EventResponse> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken
    ) =>
        _eventService.GetByIdAsync(
            eventId: request.EventId,
            cancellationToken: cancellationToken
        );
}
