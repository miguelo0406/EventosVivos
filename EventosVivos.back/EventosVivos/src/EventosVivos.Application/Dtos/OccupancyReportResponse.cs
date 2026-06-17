namespace EventosVivos.Application.Dtos;

public sealed record OccupancyReportResponse
{
    public required Guid EventId { get; init; }

    public required string EventTitle { get; init; }

    public required string VenueName { get; init; }

    public required int TotalSoldTickets { get; init; }

    public required int TotalAvailableTickets { get; init; }

    public required double OccupancyPercentage { get; init; }

    public required decimal TotalRevenue { get; init; }

    public required string EventStatus { get; init; }
}
