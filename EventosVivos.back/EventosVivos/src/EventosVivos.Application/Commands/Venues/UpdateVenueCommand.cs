using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Venues;

// CQRS — Command: actualiza un venue existente.
public sealed record UpdateVenueCommand(
    int VenueId,
    UpdateVenueRequest Request
) : IRequest<VenueResponse>;
