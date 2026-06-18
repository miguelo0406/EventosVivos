using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Queries.Venues;

internal sealed class ListVenuesQueryHandler : IRequestHandler<ListVenuesQuery, IReadOnlyList<VenueResponse>>
{
    private readonly IVenueService _venueService;

    public ListVenuesQueryHandler(IVenueService venueService)
    {
        _venueService = venueService;
    }

    public Task<IReadOnlyList<VenueResponse>> Handle(
        ListVenuesQuery request,
        CancellationToken cancellationToken
    ) =>
        _venueService.GetAllAsync(cancellationToken: cancellationToken);
}
