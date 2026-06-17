using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.UseCases;

public sealed class GetEventByIdUseCase : IGetEventByIdUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IClock _clock;

    public GetEventByIdUseCase(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _clock = clock;
    }

    public async Task<EventResponse> ExecuteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var targetEvent = await _eventRepository.GetByIdAsync(id: eventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: eventId);

        var venue = await _venueRepository.GetByIdAsync(id: targetEvent.VenueId, cancellationToken: cancellationToken)
            ?? throw new VenueNotFoundException(venueId: targetEvent.VenueId);

        return EventResponseMapper.ToResponse(targetEvent: targetEvent, venue: venue, currentTime: _clock.UtcNow);
    }
}
