using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.ValueObjects;

// Value Object de solo lectura (RF-06): representa una "foto" calculada del estado de
// ocupación de un evento en un instante dado. No tiene identidad propia ni se
// persiste; se recalcula en cada consulta.
public sealed record OccupancyReport
{
    public required Guid EventId { get; init; }

    public required int TotalSoldTickets { get; init; }

    public required int TotalAvailableTickets { get; init; }

    public required double OccupancyPercentage { get; init; }

    public required decimal TotalRevenue { get; init; }

    public required EventStatus EventStatus { get; init; }
}
