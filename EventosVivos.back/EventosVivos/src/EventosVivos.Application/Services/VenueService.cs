using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Services.Interfaces;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Services;

// Responsabilidad única (S de SOLID): orquesta los casos de uso del agregado Venue. La
// validación de forma (nombre/capacidad/ciudad) vive en el dominio (Venue.Create/Update);
// este servicio añade los guardas que cruzan con Event (capacidad mínima ya comprometida,
// venue referenciado) porque solo él puede consultar ambos agregados.
public sealed class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;

    public VenueService(IVenueRepository venueRepository, IEventRepository eventRepository)
    {
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IReadOnlyList<VenueResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var venues = await _venueRepository.GetAllAsync(cancellationToken: cancellationToken);

        return venues.Select(VenueResponseMapper.ToResponse).ToList();
    }

    public async Task<VenueResponse> CreateAsync(CreateVenueRequest request, CancellationToken cancellationToken)
    {
        // El Id es ValueGeneratedNever (la tabla conserva el seed 1-3); se asigna max+1.
        var maxId = await _venueRepository.GetMaxIdAsync(cancellationToken: cancellationToken);

        var venue = Venue.Create(
            id: maxId + 1,
            name: request.Name,
            capacity: request.Capacity,
            city: request.City);

        await _venueRepository.AddAsync(venue: venue, cancellationToken: cancellationToken);
        await _venueRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return VenueResponseMapper.ToResponse(venue: venue);
    }

    public async Task<VenueResponse> UpdateAsync(int venueId, UpdateVenueRequest request, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetByIdAsync(id: venueId, cancellationToken: cancellationToken)
            ?? throw new VenueNotFoundException(venueId: venueId);

        // Guarda RN-01 retroactivo: la nueva capacidad no puede quedar por debajo del aforo
        // de un evento activo ya programado en este venue.
        var activeEvents = await _eventRepository.GetActiveEventsByVenueAsync(
            venueId: venueId,
            cancellationToken: cancellationToken);

        if (activeEvents.Count > 0)
        {
            var requiredCapacity = activeEvents.Max(activeEvent => activeEvent.MaxCapacity);
            if (request.Capacity < requiredCapacity)
            {
                throw new VenueCapacityBelowScheduledEventException(
                    venueId: venueId,
                    requestedCapacity: request.Capacity,
                    requiredCapacity: requiredCapacity);
            }
        }

        venue.Update(name: request.Name, capacity: request.Capacity, city: request.City);

        await _venueRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return VenueResponseMapper.ToResponse(venue: venue);
    }

    public async Task DeleteAsync(int venueId, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetByIdAsync(id: venueId, cancellationToken: cancellationToken)
            ?? throw new VenueNotFoundException(venueId: venueId);

        // Guarda de integridad: no se borra un venue al que aún apunta algún evento.
        var hasEvents = await _eventRepository.AnyByVenueAsync(venueId: venueId, cancellationToken: cancellationToken);
        if (hasEvents)
        {
            throw new VenueInUseException(venueId: venueId);
        }

        _venueRepository.Remove(venue: venue);
        await _venueRepository.SaveChangesAsync(cancellationToken: cancellationToken);
    }
}
