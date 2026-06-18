using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Venues;

internal sealed class UpdateVenueCommandHandler : IRequestHandler<UpdateVenueCommand, VenueResponse>
{
    private readonly IVenueService _venueService;

    public UpdateVenueCommandHandler(IVenueService venueService)
    {
        _venueService = venueService;
    }

    public Task<VenueResponse> Handle(
        UpdateVenueCommand request,
        CancellationToken cancellationToken
    ) =>
        _venueService.UpdateAsync(
            venueId: request.VenueId,
            request: request.Request,
            cancellationToken: cancellationToken
        );
}
