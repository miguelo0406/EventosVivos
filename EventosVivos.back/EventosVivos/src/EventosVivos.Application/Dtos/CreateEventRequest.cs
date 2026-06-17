namespace EventosVivos.Application.Dtos;

public sealed record CreateEventRequest
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required int VenueId { get; init; }

    public required int MaxCapacity { get; init; }

    public required DateTime StartDateTime { get; init; }

    public required DateTime EndDateTime { get; init; }

    public required decimal TicketPrice { get; init; }

    public required string Type { get; init; }
}
