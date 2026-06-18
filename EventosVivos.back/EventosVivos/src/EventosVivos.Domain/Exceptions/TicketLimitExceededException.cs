namespace EventosVivos.Domain.Exceptions;

// RN-05 (precio > $100 ⇒ máx. 10 entradas) y regla de RF-03 (evento en menos de 24 h
// ⇒ máx. 5 entradas). Se aplica siempre el límite más restrictivo.
public sealed class TicketLimitExceededException : DomainException
{
    public TicketLimitExceededException(
        int requestedQuantity,
        int maxAllowed
    )
        : base(
            message: $"La cantidad solicitada ({requestedQuantity}) excede el máximo permitido por transacción ({maxAllowed}).",
            errorCode: "TICKET_LIMIT_EXCEEDED"
        )
    {
    }
}
