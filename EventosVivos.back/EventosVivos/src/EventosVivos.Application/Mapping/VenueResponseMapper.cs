using EventosVivos.Application.Dtos;
using EventosVivos.Domain.Entities;

namespace EventosVivos.Application.Mapping;

public static class VenueResponseMapper
{
    public static VenueResponse ToResponse(Venue venue)
    {
        return new()
        {
            Id = venue.Id,
            Name = venue.Name,
            Capacity = venue.Capacity,
            City = venue.City,
        };
    }
}
