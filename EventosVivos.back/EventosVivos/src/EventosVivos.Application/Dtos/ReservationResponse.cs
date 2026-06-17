namespace EventosVivos.Application.Dtos;

public sealed record ReservationResponse
{
    public required Guid Id { get; init; }

    public required Guid EventId { get; init; }

    public required int Quantity { get; init; }

    public required string BuyerName { get; init; }

    public required string BuyerEmail { get; init; }

    public required string Status { get; init; }

    public string? ConfirmationCode { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? ConfirmedAt { get; init; }

    public DateTime? CancelledAt { get; init; }
}
