using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Exceptions;

public sealed class ReservationNotFoundException : DomainException
{
    public ReservationNotFoundException(Guid reservationId)
        : base(
            message: $"No se encontró la reserva '{reservationId}'.",
            errorCode: "RESERVATION_NOT_FOUND")
    {
    }
}
