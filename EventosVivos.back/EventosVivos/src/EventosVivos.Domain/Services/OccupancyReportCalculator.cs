using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Services;

// Servicio de dominio (RF-06). Principio de abierto/cerrado (O de SOLID): si se agrega
// una nueva métrica al reporte, este método se extiende sin modificar Event ni
// Reservation, que permanecen cerrados a cambios por requisitos de reporting.
public static class OccupancyReportCalculator
{
    public static OccupancyReport Calculate(
        Event targetEvent,
        IEnumerable<Reservation> reservations,
        DateTime currentTime)
    {
        var reservationList = reservations.ToList();

        var confirmedTickets = reservationList
            .Where(reservation => reservation.Status == ReservationStatus.Confirmed)
            .Sum(reservation => reservation.Quantity);

        var availableTickets = TicketAvailabilityCalculator.GetAvailableTickets(
            maxCapacity: targetEvent.MaxCapacity,
            existingReservations: reservationList);

        var occupancyPercentage = targetEvent.MaxCapacity == 0
            ? 0d
            : Math.Round(confirmedTickets / (double)targetEvent.MaxCapacity * 100, digits: 2);

        return new()
        {
            EventId = targetEvent.Id,
            TotalSoldTickets = confirmedTickets,
            TotalAvailableTickets = availableTickets,
            OccupancyPercentage = occupancyPercentage,
            TotalRevenue = confirmedTickets * targetEvent.TicketPrice,
            EventStatus = targetEvent.GetEffectiveStatus(currentTime: currentTime),
        };
    }
}
