namespace EventosVivos.Application.Dtos;

public sealed record ReserveTicketsRequest
{
    public required Guid EventId { get; init; }

    public required int Quantity { get; init; }

    public required string BuyerName { get; init; }

    public required string BuyerEmail { get; init; }
}
