using EventosVivos.Domain.Entities;

namespace EventosVivos.Domain.Services;

// Servicio de dominio sin estado: encapsula el algoritmo de detección de
// superposición de horarios (RN-02). Vive en Domain porque es lógica de negocio pura
// (no requiere I/O); la capa Application es responsable de obtener, vía
// IEventRepository, los eventos existentes contra los cuales se compara.
// Responsabilidad única (S de SOLID): esta clase solo compara intervalos de tiempo,
// no sabe cómo obtener los eventos ni cómo persistirlos.
public static class VenueScheduleConflictChecker
{
    public static bool HasConflict(
        DateTime candidateStart,
        DateTime candidateEnd,
        IEnumerable<Event> existingActiveEventsAtVenue
    )
    {
        return existingActiveEventsAtVenue.Any(existingEvent =>
            candidateStart < existingEvent.EndDateTime && existingEvent.StartDateTime < candidateEnd);
    }
}