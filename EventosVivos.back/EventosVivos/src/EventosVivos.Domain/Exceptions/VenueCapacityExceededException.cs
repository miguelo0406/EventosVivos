namespace EventosVivos.Domain.Exceptions;

// RN-01: un evento no puede exceder la capacidad del venue asignado.
public sealed class VenueCapacityExceededException : DomainException
{
    public VenueCapacityExceededException(
        int requestedCapacity,
        int venueCapacity
    )
        : base(
            message: $"La capacidad solicitada ({requestedCapacity}) excede la capacidad del venue ({venueCapacity}).",
            errorCode: "VENUE_CAPACITY_EXCEEDED"
        )
    {
    }
}
