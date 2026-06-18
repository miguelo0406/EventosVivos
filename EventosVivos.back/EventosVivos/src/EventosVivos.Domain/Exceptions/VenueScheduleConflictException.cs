namespace EventosVivos.Domain.Exceptions;

// RN-02: dos eventos activos no pueden compartir el mismo venue con horarios
// superpuestos.
public sealed class VenueScheduleConflictException : DomainException
{
    public VenueScheduleConflictException(int venueId)
        : base(
            message: $"El venue {venueId} ya tiene un evento activo en ese horario.",
            errorCode: "VENUE_SCHEDULE_CONFLICT"
        )
    {
    }
}
