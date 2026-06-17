namespace EventosVivos.Domain.Enums;

/// <summary>
/// Estado de un evento. <see cref="Completed"/> es un estado derivado en tiempo de
/// consulta (RN-06): no se persiste, se calcula comparando la hora actual contra
/// <c>EndDateTime</c> cuando el estado persistido es <see cref="Active"/>.
/// </summary>
public enum EventStatus
{
    Active,
    Cancelled,
    Completed,
}
