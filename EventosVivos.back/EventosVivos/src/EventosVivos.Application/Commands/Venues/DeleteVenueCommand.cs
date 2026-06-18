using MediatR;

namespace EventosVivos.Application.Commands.Venues;

// CQRS — Command: elimina un venue. No retorna payload (204 NoContent), por eso usa Unit.
public sealed record DeleteVenueCommand(
    int VenueId
) : IRequest<Unit>;