namespace EventosVivos.Domain.Exceptions;

// RF-03: valida que el email del comprador tenga un formato válido.
public sealed class InvalidEmailFormatException : DomainException
{
    public InvalidEmailFormatException(string invalidValue)
        : base(
            message: $"El email '{invalidValue}' no tiene un formato válido.",
            errorCode: "INVALID_EMAIL_FORMAT")
    {
    }
}
