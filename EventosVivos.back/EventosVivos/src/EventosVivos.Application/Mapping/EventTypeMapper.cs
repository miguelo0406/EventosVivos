using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Mapping;

// Traduce el enum de Domain (nombrado en inglés por convención) a los literales en
// español que exige el contrato de la API (RF-01: "conferencia", "taller",
// "concierto") y viceversa. Mantiene a Domain libre de cualquier detalle de
// serialización.
public static class EventTypeMapper
{
    public static string ToWireFormat(EventType type) => type switch
    {
        EventType.Conference => "conferencia",
        EventType.Workshop => "taller",
        EventType.Concert => "concierto",
        _ => throw new ArgumentOutOfRangeException(paramName: nameof(type), actualValue: type, message: "Tipo de evento no soportado."),
    };

    public static EventType FromWireFormat(string value) => value.Trim().ToLowerInvariant() switch
    {
        "conferencia" => EventType.Conference,
        "taller" => EventType.Workshop,
        "concierto" => EventType.Concert,
        _ => throw new DomainValidationException(
            errors: [$"Tipo de evento inválido: '{value}'. Valores permitidos: conferencia, taller, concierto."]),
    };
}
