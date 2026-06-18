namespace EventosVivos.Domain.Exceptions;

// RF-03: valida que existan entradas disponibles antes de crear la reserva (no
// exceder el aforo del evento).
public sealed class InsufficientTicketsAvailableException : DomainException
{
    public InsufficientTicketsAvailableException(
        int requestedQuantity,
        int availableTickets
    )
        : base(
            message: $"Solo quedan {availableTickets} entradas disponibles; se solicitaron {requestedQuantity}.",
            errorCode: "INSUFFICIENT_TICKETS_AVAILABLE"
        )
    {
    }
}
