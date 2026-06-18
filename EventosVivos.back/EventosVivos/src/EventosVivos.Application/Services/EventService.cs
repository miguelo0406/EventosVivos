using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Services.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Ports;
using EventosVivos.Domain.Services;

namespace EventosVivos.Application.Services;

// Responsabilidad única (S de SOLID): orquestación de los casos de uso del agregado
// Event. La lógica de invariantes vive en el dominio (Event.Create, servicios de
// dominio); este servicio solo coordina puertos (repositorios, reloj) y traduce a DTOs.
public sealed class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;

    public EventService(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IReservationRepository reservationRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _reservationRepository = reservationRepository;
        _clock = clock;
    }

    // RF-01.
    public async Task<EventResponse> CreateAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken
    )
    {
        var venue = await _venueRepository.GetByIdAsync(
                        id: request.VenueId, cancellationToken: cancellationToken
                    )
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
            currentTime: currentTime
        );

        await _eventRepository.AddAsync(eventToAdd: newEvent, cancellationToken: cancellationToken);

        await _eventRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return EventResponseMapper.ToResponse(targetEvent: newEvent, venue: venue, currentTime: currentTime);
    }

    // RF-02.
    public async Task<IReadOnlyList<EventResponse>> SearchAsync(EventSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var currentTime = _clock.UtcNow;

        var events = await _eventRepository.SearchAsync(
            filter: filter,
            currentTime: currentTime,
            cancellationToken: cancellationToken
        );

        var venues = await _venueRepository.GetAllAsync(
            cancellationToken: cancellationToken
        );
        var venuesById = venues.ToDictionary(venue => venue.Id);

        return events
            .Select(matchedEvent => EventResponseMapper.ToResponse(
                    targetEvent: matchedEvent,
                    venue: venuesById[matchedEvent.VenueId],
                    currentTime: currentTime
                )
            )
            .ToList();
    }

    public async Task<EventResponse> GetByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken
    )
    {
        var targetEvent = await _eventRepository.GetByIdAsync(
                              id: eventId,
                              cancellationToken: cancellationToken
                          )
                          ?? throw new EventNotFoundException(eventId: eventId);

        var venue = await _venueRepository.GetByIdAsync(
                        id: targetEvent.VenueId,
                        cancellationToken: cancellationToken
                    )
                    ?? throw new VenueNotFoundException(venueId: targetEvent.VenueId);

        return EventResponseMapper.ToResponse(targetEvent: targetEvent, venue: venue, currentTime: _clock.UtcNow);
    }

    public async Task CancelAsync(
        Guid eventId,
        CancellationToken cancellationToken
    )
    {
        var targetEvent = await _eventRepository.GetByIdAsync(id: eventId, cancellationToken: cancellationToken)
                          ?? throw new EventNotFoundException(eventId: eventId);

        targetEvent.Cancel(currentTime: _clock.UtcNow);

        await _eventRepository.SaveChangesAsync(cancellationToken: cancellationToken);
    }

    // RF-06.
    public async Task<OccupancyReportResponse> GetOccupancyReportAsync(
        Guid eventId,
        CancellationToken cancellationToken
    )
    {
        var targetEvent = await _eventRepository.GetByIdAsync(
                              id: eventId,
                              cancellationToken: cancellationToken
                          )
                          ?? throw new EventNotFoundException(eventId: eventId);

        var venue = await _venueRepository.GetByIdAsync(
                        id: targetEvent.VenueId,
                        cancellationToken: cancellationToken
                    )
                    ?? throw new VenueNotFoundException(venueId: targetEvent.VenueId);

        var reservations = await _reservationRepository.GetByEventIdAsync(
            eventId: eventId,
            cancellationToken: cancellationToken
        );

        var report = OccupancyReportCalculator.Calculate(
            targetEvent: targetEvent,
            reservations: reservations,
            currentTime: _clock.UtcNow
        );

        return new()
        {
            EventId = report.EventId,
            EventTitle = targetEvent.Title,
            VenueName = venue.Name,
            TotalSoldTickets = report.TotalSoldTickets,
            TotalAvailableTickets = report.TotalAvailableTickets,
            OccupancyPercentage = report.OccupancyPercentage,
            TotalRevenue = report.TotalRevenue,
            EventStatus = EventStatusMapper.ToWireFormat(status: report.EventStatus),
        };
    }
}