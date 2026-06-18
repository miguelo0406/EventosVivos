namespace EventosVivos.Application.Dtos;

public sealed record RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public string? FullName { get; init; }
}
