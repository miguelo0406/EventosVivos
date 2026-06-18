namespace EventosVivos.Application.Dtos;

public sealed record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
