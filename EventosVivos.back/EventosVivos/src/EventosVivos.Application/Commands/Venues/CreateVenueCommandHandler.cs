using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Venues;

// Handler delgado: delega en el servicio/facade. Internal: MediatR lo descubre por escaneo.
internal sealed class CreateVenueCommandHandler : IRequestHandler<CreateVenueCommand, VenueResponse>
{
    private readonly IVenueService _venueService;

    public CreateVenueCommandHandler(IVenueService venueService)
    {
        _venueService = venueService;
    }

    public Task<VenueResponse> Handle(
        CreateVenueCommand request,
        CancellationToken cancellationToken
    ) =>
        _venueService.CreateAsync(
            request: request.Request,
            cancellationToken: cancellationToken
        );
}
