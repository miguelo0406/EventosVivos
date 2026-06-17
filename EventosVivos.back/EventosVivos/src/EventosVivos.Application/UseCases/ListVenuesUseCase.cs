using EventosVivos.Application.Dtos;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.UseCases;

public sealed class ListVenuesUseCase : IListVenuesUseCase
{
    private readonly IVenueRepository _venueRepository;

    public ListVenuesUseCase(IVenueRepository venueRepository)
    {
        _venueRepository = venueRepository;
    }

    public async Task<IReadOnlyList<VenueResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var venues = await _venueRepository.GetAllAsync(cancellationToken: cancellationToken);

        return venues.Select(VenueResponseMapper.ToResponse).ToList();
    }
}
