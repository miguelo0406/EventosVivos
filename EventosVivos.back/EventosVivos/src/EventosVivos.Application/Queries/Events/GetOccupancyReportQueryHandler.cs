using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Queries.Events;

internal sealed class GetOccupancyReportQueryHandler : IRequestHandler<GetOccupancyReportQuery, OccupancyReportResponse>
{
    private readonly IEventService _eventService;

    public GetOccupancyReportQueryHandler(IEventService eventService)
    {
        _eventService = eventService;
    }

    public Task<OccupancyReportResponse> Handle(
        GetOccupancyReportQuery request,
        CancellationToken cancellationToken
    ) =>
        _eventService.GetOccupancyReportAsync(
            eventId: request.EventId,
            cancellationToken: cancellationToken
        );
}
