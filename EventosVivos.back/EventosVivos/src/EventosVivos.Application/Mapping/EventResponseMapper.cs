using EventosVivos.Application.Dtos;
using EventosVivos.Domain.Entities;

namespace EventosVivos.Application.Mapping;

public static class EventResponseMapper
{
    public static EventResponse ToResponse(Event targetEvent, Venue venue, DateTime currentTime)
    {
        return new()
        {
            Id = targetEvent.Id,
            Title = targetEvent.Title,
            Description = targetEvent.Description,
            VenueId = targetEvent.VenueId,
            VenueName = venue.Name,
            MaxCapacity = targetEvent.MaxCapacity,
            StartDateTime = targetEvent.StartDateTime,
            EndDateTime = targetEvent.EndDateTime,
            TicketPrice = targetEvent.TicketPrice,
            Type = EventTypeMapper.ToWireFormat(type: targetEvent.Type),
            Status = EventStatusMapper.ToWireFormat(status: targetEvent.GetEffectiveStatus(currentTime: currentTime)),
            CreatedAt = targetEvent.CreatedAt,
        };
    }
}
