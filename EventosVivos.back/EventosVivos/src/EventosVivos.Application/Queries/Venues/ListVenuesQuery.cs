using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Queries.Venues;

// CQRS — Query: venues de referencia para el formulario de creación de eventos (RF-01).
public sealed record ListVenuesQuery() : IRequest<IReadOnlyList<VenueResponse>>;
