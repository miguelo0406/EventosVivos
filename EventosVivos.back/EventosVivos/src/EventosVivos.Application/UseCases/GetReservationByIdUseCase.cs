using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.UseCases;

public sealed class GetReservationByIdUseCase : IGetReservationByIdUseCase
{
    private readonly IReservationRepository _reservationRepository;

    public GetReservationByIdUseCase(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<ReservationResponse> ExecuteAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id: reservationId, cancellationToken: cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId: reservationId);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }
}
