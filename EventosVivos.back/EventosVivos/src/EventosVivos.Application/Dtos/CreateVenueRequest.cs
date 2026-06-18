namespace EventosVivos.Application.Dtos;

public sealed record CreateVenueRequest
{
    public required string Name { get; init; }

    public required int Capacity { get; init; }

    public required string City { get; init; }
}
