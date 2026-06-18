using EventosVivos.Application.Dtos;

namespace EventosVivos.Application.Services.Interfaces;

// Facade/servicio de aplicación del agregado Reservation. Concentra la orquestación de
// RF-03 a RF-05; los handlers de MediatR delegan aquí.
public interface IReservationService
{
    Task<ReservationResponse> ReserveAsync(
        ReserveTicketsRequest request,
        CancellationToken cancellationToken
    );

    Task<ReservationResponse> GetByIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken
    );

    Task<ReservationResponse> ConfirmPaymentAsync(
        Guid reservationId,
        CancellationToken cancellationToken
    );

    Task<ReservationResponse> CancelAsync(
        Guid reservationId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ReservationResponse>> GetByEventAsync(
        Guid eventId,
        CancellationToken cancellationToken
    );
}