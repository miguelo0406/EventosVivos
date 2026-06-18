using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Exceptions;

// Credenciales inválidas en el login (se mapea a 401).
public sealed class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(
        string message = "Credenciales inválidas."
    )
        : base(
            message: message,
            errorCode: "AUTHENTICATION_FAILED"
        )
    {
    }
}