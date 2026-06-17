using EventosVivos.Domain.Enums;

namespace EventosVivos.Application.Mapping;

public static class ReservationStatusMapper
{
    public static string ToWireFormat(ReservationStatus status) => status switch
    {
        ReservationStatus.PendingPayment => "pendiente_pago",
        ReservationStatus.Confirmed => "confirmada",
        ReservationStatus.Cancelled => "cancelada",
        ReservationStatus.Lost => "perdida",
        _ => throw new ArgumentOutOfRangeException(paramName: nameof(status), actualValue: status, message: "Estado de reserva no soportado."),
    };
}
