namespace EventosVivos.Domain.Exceptions;

// Agrupa todos los errores de validación de campos (RF-01) encontrados en una sola
// operación, en vez de fallar con el primer error: mejora la experiencia de quien
// consume la API al mostrar todos los problemas de una vez.
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(IReadOnlyCollection<string> errors)
        : base(
            message: "Se encontraron errores de validación.",
            errorCode: "VALIDATION_ERROR"
        )
    {
        Errors = errors;
    }

    public IReadOnlyCollection<string> Errors { get; }
}