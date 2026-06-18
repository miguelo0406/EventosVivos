using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Exceptions;

// Caso borde del CRUD de venues: reducir la capacidad de un venue por debajo del aforo de
// un evento ya programado en él rompería RN-01 retroactivamente. Se rechaza con 409.
public sealed class VenueCapacityBelowScheduledEventException : DomainException
{
    public VenueCapacityBelowScheduledEventException(
        int venueId,
        int requestedCapacity,
        int requiredCapacity
    )
        : base(
            message: $"No se puede reducir la capacidad del venue '{venueId}' a {requestedCapacity}: " +
                     $"ya hay un evento programado que requiere al menos {requiredCapacity}.",
            errorCode: "VENUE_CAPACITY_BELOW_SCHEDULED_EVENT"
        )
    {
    }
}
