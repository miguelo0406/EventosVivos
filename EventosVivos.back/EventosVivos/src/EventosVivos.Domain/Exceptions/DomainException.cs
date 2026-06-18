namespace EventosVivos.Domain.Exceptions;

// Clase base de las excepciones de dominio: permite que la capa Api distinga errores
// de negocio (4xx, con código de error legible por máquina) de errores técnicos no
// controlados (5xx) mediante un único middleware de manejo de excepciones.
public abstract class DomainException : Exception
{
    protected DomainException(
        string message,
        string errorCode
    )
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
