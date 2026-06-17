using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.UseCases;

public sealed class CancelEventUseCase : ICancelEventUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public CancelEventUseCase(IEventRepository eventRepository, IClock clock)
    {
        _eventRepository = eventRepository;
        _clock = clock;
    }

    public async Task ExecuteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var targetEvent = await _eventRepository.GetByIdAsync(id: eventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: eventId);

        targetEvent.Cancel(currentTime: _clock.UtcNow);

        await _eventRepository.SaveChangesAsync(cancellationToken: cancellationToken);
    }
}
