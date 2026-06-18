using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Queries.Events;

// CQRS — Query (RF-06).
public sealed record GetOccupancyReportQuery(
    Guid EventId
) : IRequest<OccupancyReportResponse>;