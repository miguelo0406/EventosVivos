using EventosVivos.Application.Dtos;
using EventosVivos.Domain.Entities;

namespace EventosVivos.Application.Mapping;

public static class ReservationResponseMapper
{
    public static ReservationResponse ToResponse(Reservation reservation)
    {
        return new()
        {
            Id = reservation.Id,
            EventId = reservation.EventId,
            Quantity = reservation.Quantity,
            BuyerName = reservation.BuyerName,
            BuyerEmail = reservation.BuyerEmail.Value,
            Status = ReservationStatusMapper.ToWireFormat(status: reservation.Status),
            ConfirmationCode = reservation.ConfirmationCode?.Value,
            CreatedAt = reservation.CreatedAt,
            ConfirmedAt = reservation.ConfirmedAt,
            CancelledAt = reservation.CancelledAt,
        };
    }
}
