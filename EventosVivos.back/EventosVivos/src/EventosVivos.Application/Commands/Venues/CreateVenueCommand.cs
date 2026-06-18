using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Venues;

// CQRS — Command (CRUD de venues, valor agregado): intención de crear un venue.
public sealed record CreateVenueCommand(
    CreateVenueRequest Request
) : IRequest<VenueResponse>;