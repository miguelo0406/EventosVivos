namespace EventosVivos.Domain.Exceptions;

// RN-04: no se permiten reservas para eventos que inicien en menos de 1 hora.
public sealed class LateReservationException : DomainException
{
    public LateReservationException(TimeSpan timeUntilEventStart)
        : base(
            message: $"No se permiten reservas: el evento inicia en {timeUntilEventStart.TotalMinutes:F0} minutos (mínimo 60).",
            errorCode: "LATE_RESERVATION"
        )
    {
    }
}
