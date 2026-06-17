using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Services;

// Servicio de dominio: calcula entradas disponibles a partir del aforo máximo y las
// reservas existentes. Cuentan contra el aforo las reservas pendientes de pago,
// confirmadas y "perdidas" (RN-07); solo una cancelación regular libera cupo.
// Responsabilidad única (S de SOLID): solo sabe sumar/restar cupos.
public static class TicketAvailabilityCalculator
{
    private static readonly ReservationStatus[] OccupyingStatuses =
    [
        ReservationStatus.PendingPayment,
        ReservationStatus.Confirmed,
        ReservationStatus.Lost,
    ];

    public static int GetAvailableTickets(int maxCapacity, IEnumerable<Reservation> existingReservations)
    {
        var occupiedTickets = existingReservations
            .Where(reservation => OccupyingStatuses.Contains(reservation.Status))
            .Sum(reservation => reservation.Quantity);

        return Math.Max(maxCapacity - occupiedTickets, 0);
    }
}
