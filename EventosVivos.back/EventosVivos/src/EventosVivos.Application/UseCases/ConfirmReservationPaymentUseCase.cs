using EventosVivos.Application.Dtos;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Application.UseCases;

// RF-04. Responsabilidad única: confirma el pago y garantiza la unicidad del código
// de reserva, algo que Domain (ConfirmationCode) no puede verificar por sí solo
// porque requiere consultar el repositorio.
public sealed class ConfirmReservationPaymentUseCase : IConfirmReservationPaymentUseCase
{
    private const int MaxGenerationAttempts = 5;

    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;

    public ConfirmReservationPaymentUseCase(IReservationRepository reservationRepository, IClock clock)
    {
        _reservationRepository = reservationRepository;
        _clock = clock;
    }

    public async Task<ReservationResponse> ExecuteAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id: reservationId, cancellationToken: cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId: reservationId);

        var confirmationCode = await GenerateUniqueConfirmationCodeAsync(cancellationToken: cancellationToken);

        reservation.ConfirmPayment(confirmationCode: confirmationCode, currentTime: _clock.UtcNow);

        await _reservationRepository.SaveChangesAsync(cancellationToken: cancellationToken);

        return ReservationResponseMapper.ToResponse(reservation: reservation);
    }

    // RF-04: "generar un código de reserva único". Se reintenta ante una colisión
    // (extremadamente improbable: 1 en 1.000.000 combinaciones por intento).
    private async Task<ConfirmationCode> GenerateUniqueConfirmationCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var candidate = ConfirmationCode.Generate();
            var alreadyExists = await _reservationRepository.ExistsByConfirmationCodeAsync(
                confirmationCode: candidate.Value,
                cancellationToken: cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(message: "No fue posible generar un código de reserva único.");
    }
}
