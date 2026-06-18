using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Exceptions;

// Caso borde del CRUD de venues: no se puede borrar un venue al que ya apunta algún evento
// (la FK es Restrict). Se traduce a 409 Conflict en el middleware.
public sealed class VenueInUseException : DomainException
{
    public VenueInUseException(int venueId)
        : base(
            message: $"No se puede eliminar el venue '{venueId}' porque tiene eventos asociados.",
            errorCode: "VENUE_IN_USE"
        )
    {
    }
}
