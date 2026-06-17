using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Ports;
using EventosVivos.Domain.Services;

namespace EventosVivos.Application.UseCases;

// Responsabilidad única (S de SOLID): esta clase solo orquesta la creación de un
// evento (buscar venue, validar superposición de horario, delegar las invariantes de
// campo a Event.Create, persistir). No conoce HTTP ni Entity Framework: depende
// exclusivamente de los puertos definidos en Domain (Inversión de dependencias, D de
// SOLID), lo que la hace testeable con dobles de prueba en memoria.
public sealed class CreateEventUseCase : ICreateEventUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IClock _clock;

    public CreateEventUseCase(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _clock = clock;
    }

    public async Task<EventResponse> ExecuteAsync(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetByIdAsync(id: request.VenueId, cancellationToken: cancellationToken)
            ?? throw new VenueNotFoundException(venueId: request.VenueId);

        var currentTime = _clock.UtcNow;

        // RN-02: dos eventos activos no pueden compartir venue con horarios superpuestos.
        var existingEvents = await _eventRepository.GetActiveEventsByVenueAsync(
            venueId: request.VenueId,
            cancellationToken: cancellationToken);

        var hasScheduleConflict = VenueScheduleConflictChecker.HasConflict(
            candidateStart: request.StartDateTime,
            candidateEnd: request.EndDateTime,
            existingActiveEventsAtVenue: existingEvents);
        if (hasScheduleConflict)
        {
            throw new VenueScheduleConflictException(venueId: request.VenueId);
        }

        var newEvent = Event.Create(
            title: request.Title,
            description: request.Description,
            venueId: request.VenueId,
            venueCapacity: venue.Capacity,
            maxCapacity: request.MaxCapacity,
            startDateTime: request.StartDateTime,
            endDateTime: request.EndDateTime,
            ticketPrice: request.TicketPrice,
            type: EventTypeMapper.FromWireFormat(value: request.Type),
            currentTime: currentTime);

        await _eventRepository.AddAsync(eventToAdd: newEvent, cancellationToken: cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return EventResponseMapper.ToResponse(targetEvent: newEvent, venue: venue, currentTime: currentTime);
    }
}
