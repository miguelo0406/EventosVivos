namespace EventosVivos.Domain.Exceptions;

// RF-04 y RF-05: rechaza transiciones de estado inválidas (confirmar una reserva ya
// confirmada o cancelada, cancelar una reserva ya cancelada, etc.).
public sealed class InvalidReservationStateTransitionException : DomainException
{
    public InvalidReservationStateTransitionException(string message)
        : base(
            message: message,
            errorCode: "INVALID_RESERVATION_STATE_TRANSITION")
    {
    }
}
