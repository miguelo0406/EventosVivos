using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Exceptions;

// El registro no pudo completarse en el IdP (p. ej. el email ya existe o la contraseña
// no cumple la política). Se mapea a 409 Conflict.
public sealed class RegistrationFailedException : DomainException
{
    public RegistrationFailedException(string message)
        : base(
            message: message,
            errorCode: "REGISTRATION_FAILED"
        )
    {
    }
}
