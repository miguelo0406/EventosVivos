namespace EventosVivos.Application.Dtos;

public sealed record VenueResponse
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int Capacity { get; init; }

    public required string City { get; init; }
}
