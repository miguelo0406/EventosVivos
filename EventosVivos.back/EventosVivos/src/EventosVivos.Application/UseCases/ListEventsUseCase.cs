using EventosVivos.Application.Dtos;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.UseCases;

// RF-02. Responsabilidad única: orquesta búsqueda + enriquecimiento con el nombre del
// venue; el filtrado en sí vive en Infrastructure (traducción a SQL) e Domain
// (EventSearchFilter, el contrato de la búsqueda).
public sealed class ListEventsUseCase : IListEventsUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IClock _clock;

    public ListEventsUseCase(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _clock = clock;
    }

    public async Task<IReadOnlyList<EventResponse>> ExecuteAsync(EventSearchFilter filter, CancellationToken cancellationToken)
    {
        var currentTime = _clock.UtcNow;

        var events = await _eventRepository.SearchAsync(
            filter: filter,
            currentTime: currentTime,
            cancellationToken: cancellationToken);

        var venues = await _venueRepository.GetAllAsync(cancellationToken: cancellationToken);
        var venuesById = venues.ToDictionary(venue => venue.Id);

        return events
            .Select(matchedEvent => EventResponseMapper.ToResponse(
                targetEvent: matchedEvent,
                venue: venuesById[matchedEvent.VenueId],
                currentTime: currentTime))
            .ToList();
    }
}
