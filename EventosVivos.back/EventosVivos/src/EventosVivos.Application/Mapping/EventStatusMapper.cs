using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Application.Mapping;

public static class EventStatusMapper
{
    public static string ToWireFormat(EventStatus status) => status switch
    {
        EventStatus.Active => "activo",
        EventStatus.Cancelled => "cancelado",
        EventStatus.Completed => "completado",
        _ => throw new ArgumentOutOfRangeException(paramName: nameof(status), actualValue: status, message: "Estado de evento no soportado."),
    };

    public static EventStatus FromWireFormat(string value) => value.Trim().ToLowerInvariant() switch
    {
        "activo" => EventStatus.Active,
        "cancelado" => EventStatus.Cancelled,
        "completado" => EventStatus.Completed,
        _ => throw new DomainValidationException(
            errors: [$"Estado de evento inválido: '{value}'. Valores permitidos: activo, cancelado, completado."]),
    };
}
