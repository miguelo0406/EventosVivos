using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Queries.Events;

internal sealed class ListEventsQueryHandler : IRequestHandler<ListEventsQuery, IReadOnlyList<EventResponse>>
{
    private readonly IEventService _eventService;

    public ListEventsQueryHandler(IEventService eventService)
    {
        _eventService = eventService;
    }

    public Task<IReadOnlyList<EventResponse>> Handle(
        ListEventsQuery request,
        CancellationToken cancellationToken
    ) =>
        _eventService.SearchAsync(
            filter: request.Filter,
            cancellationToken: cancellationToken
        );
}
