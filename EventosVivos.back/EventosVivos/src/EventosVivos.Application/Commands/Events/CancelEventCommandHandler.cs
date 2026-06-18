using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Events;

internal sealed class CancelEventCommandHandler : IRequestHandler<CancelEventCommand, Unit>
{
    private readonly IEventService _eventService;

    public CancelEventCommandHandler(IEventService eventService)
    {
        _eventService = eventService;
    }

    public async Task<Unit> Handle(
        CancelEventCommand request,
        CancellationToken cancellationToken
    )
    {
        await _eventService.CancelAsync(
            eventId: request.EventId,
            cancellationToken: cancellationToken
        );

        return Unit.Value;
    }
}
