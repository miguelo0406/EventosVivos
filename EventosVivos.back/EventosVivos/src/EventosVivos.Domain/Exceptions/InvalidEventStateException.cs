namespace EventosVivos.Domain.Exceptions;

// Rechaza operaciones inválidas sobre el estado de un evento (por ejemplo, cancelar un
// evento ya cancelado o completado).
public sealed class InvalidEventStateException : DomainException
{
    public InvalidEventStateException(string message)
        : base(
            message: message,
            errorCode: "INVALID_EVENT_STATE"
        )
    {
    }
}
