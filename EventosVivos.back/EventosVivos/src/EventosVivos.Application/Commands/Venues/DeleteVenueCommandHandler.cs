using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Venues;

internal sealed class DeleteVenueCommandHandler : IRequestHandler<DeleteVenueCommand, Unit>
{
    private readonly IVenueService _venueService;

    public DeleteVenueCommandHandler(IVenueService venueService)
    {
        _venueService = venueService;
    }

    public async Task<Unit> Handle(
        DeleteVenueCommand request,
        CancellationToken cancellationToken
    )
    {
        await _venueService.DeleteAsync(
            venueId: request.VenueId,
            cancellationToken: cancellationToken
        );

        return Unit.Value;
    }
}
