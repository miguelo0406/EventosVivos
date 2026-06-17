using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;
using EventosVivos.Domain.Services;

namespace EventosVivos.Application.UseCases;

// RF-06. Delega el cálculo numérico a OccupancyReportCalculator (Domain) y solo
// agrega los datos de presentación (título del evento, nombre del venue).
public sealed class GetOccupancyReportUseCase : IGetOccupancyReportUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IClock _clock;

    public GetOccupancyReportUseCase(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        IVenueRepository venueRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _venueRepository = venueRepository;
        _clock = clock;
    }

    public async Task<OccupancyReportResponse> ExecuteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var targetEvent = await _eventRepository.GetByIdAsync(id: eventId, cancellationToken: cancellationToken)
            ?? throw new EventNotFoundException(eventId: eventId);

        var venue = await _venueRepository.GetByIdAsync(id: targetEvent.VenueId, cancellationToken: cancellationToken)
            ?? throw new VenueNotFoundException(venueId: targetEvent.VenueId);

        var reservations = await _reservationRepository.GetByEventIdAsync(
            eventId: eventId,
            cancellationToken: cancellationToken);

        var report = OccupancyReportCalculator.Calculate(
            targetEvent: targetEvent,
            reservations: reservations,
            currentTime: _clock.UtcNow);

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
