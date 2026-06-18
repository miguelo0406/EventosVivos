using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Ports;

// Objeto de especificación de búsqueda (RF-02). Vive junto al puerto porque forma
// parte de su contrato, no es un DTO de transporte HTTP (ese mapeo ocurre en Api).
public sealed record EventSearchFilter
{
    public EventType? Type { get; init; }

    public DateTime? FromStartDate { get; init; }

    public DateTime? ToStartDate { get; init; }

    public int? VenueId { get; init; }

    public EventStatus? Status { get; init; }

    public string? TitleSearch { get; init; }
}
