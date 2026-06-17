namespace EventosVivos.Domain.Enums;

/// <summary>
/// Estado de una reserva. <see cref="Lost"/> (RN-07) representa una reserva confirmada
/// que se canceló a menos de 48 h del evento: no libera entradas, solo se registra
/// para efectos de reporte.
/// </summary>
public enum ReservationStatus
{
    PendingPayment,
    Confirmed,
    Cancelled,
    Lost,
}
