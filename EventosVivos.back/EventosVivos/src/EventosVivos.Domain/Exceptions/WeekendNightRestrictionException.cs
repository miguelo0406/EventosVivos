namespace EventosVivos.Domain.Exceptions;

// RN-03: eventos en fin de semana (sábado/domingo) no pueden iniciar después de las
// 22:00.
public sealed class WeekendNightRestrictionException : DomainException
{
    public WeekendNightRestrictionException(DateTime startDateTime)
        : base(
            message: $"Los eventos de fin de semana no pueden iniciar después de las 22:00 (solicitado: {startDateTime:HH:mm}).",
            errorCode: "WEEKEND_NIGHT_RESTRICTION"
        )
    {
    }
}
