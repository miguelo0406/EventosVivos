using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Exceptions;

public sealed class EventNotFoundException : DomainException
{
    public EventNotFoundException(Guid eventId)
        : base(
            message: $"No se encontró el evento '{eventId}'.",
            errorCode: "EVENT_NOT_FOUND")
    {
    }
}
