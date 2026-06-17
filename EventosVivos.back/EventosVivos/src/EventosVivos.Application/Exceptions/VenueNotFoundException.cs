using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Exceptions;

public sealed class VenueNotFoundException : DomainException
{
    public VenueNotFoundException(int venueId)
        : base(
            message: $"No se encontró el venue '{venueId}'.",
            errorCode: "VENUE_NOT_FOUND")
    {
    }
}
