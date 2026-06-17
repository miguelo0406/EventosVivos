using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Entities;

// Entidad raíz de agregado. Igual que Event, se construye únicamente a través de
// Create para garantizar que toda instancia respeta sus invariantes desde el momento
// en que existe.
public sealed class Reservation
{
    private Reservation()
    {
    }

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public int Quantity { get; private set; }

    public string BuyerName { get; private set; } = string.Empty;

    public Email BuyerEmail { get; private set; } = null!;

    public ReservationStatus Status { get; private set; }

    public ConfirmationCode? ConfirmationCode { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public static Reservation Create(
        Guid eventId,
        int quantity,
        string buyerName,
        string buyerEmail,
        int availableTickets,
        decimal eventTicketPrice,
        TimeSpan timeUntilEventStart,
        DateTime currentTime)
    {
        List<string> validationErrors = [];

        ValidateBuyerName(buyerName: buyerName, errors: validationErrors);
        ValidateQuantity(quantity: quantity, errors: validationErrors);

        if (!Email.TryCreate(value: buyerEmail, email: out var parsedEmail))
        {
            validationErrors.Add("El email del comprador no tiene un formato válido.");
        }

        if (validationErrors.Count > 0)
        {
            throw new DomainValidationException(errors: validationErrors);
        }

        // RN-04: no se permiten reservas para eventos que inicien en menos de 1 hora.
        if (timeUntilEventStart < TimeSpan.FromHours(1))
        {
            throw new LateReservationException(timeUntilEventStart: timeUntilEventStart);
        }

        var ticketLimit = GetTransactionTicketLimit(
            timeUntilEventStart: timeUntilEventStart,
            eventTicketPrice: eventTicketPrice);
        if (ticketLimit.HasValue && quantity > ticketLimit.Value)
        {
            throw new TicketLimitExceededException(requestedQuantity: quantity, maxAllowed: ticketLimit.Value);
        }

        // RF-03: validar que existan entradas disponibles (no exceder capacidad).
        if (quantity > availableTickets)
        {
            throw new InsufficientTicketsAvailableException(
                requestedQuantity: quantity,
                availableTickets: availableTickets);
        }

        return new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Quantity = quantity,
            BuyerName = buyerName.Trim(),
            BuyerEmail = parsedEmail!,
            Status = ReservationStatus.PendingPayment,
            CreatedAt = currentTime,
        };
    }

    // RF-04: confirma el pago y genera el código único de reserva.
    public void ConfirmPayment(ConfirmationCode confirmationCode, DateTime currentTime)
    {
        if (Status == ReservationStatus.Confirmed)
        {
            throw new InvalidReservationStateTransitionException(message: "La reserva ya está confirmada.");
        }

        if (Status is ReservationStatus.Cancelled or ReservationStatus.Lost)
        {
            throw new InvalidReservationStateTransitionException(message: "La reserva fue cancelada y no puede confirmarse.");
        }

        Status = ReservationStatus.Confirmed;
        ConfirmationCode = confirmationCode;
        ConfirmedAt = currentTime;
    }

    // RF-05 + RN-07: cancela la reserva. Si estaba confirmada y faltan menos de 48 h
    // para el evento, se penaliza marcándola como "perdida" (no libera el cupo).
    public void Cancel(DateTime currentTime, TimeSpan timeUntilEventStart)
    {
        if (Status is ReservationStatus.Cancelled or ReservationStatus.Lost)
        {
            throw new InvalidReservationStateTransitionException(message: "La reserva ya se encuentra cancelada.");
        }

        var appliesLatePenalty = Status == ReservationStatus.Confirmed
            && timeUntilEventStart < TimeSpan.FromHours(48);

        Status = appliesLatePenalty ? ReservationStatus.Lost : ReservationStatus.Cancelled;
        CancelledAt = currentTime;
    }

    // RN-05 (precio > $100 ⇒ máx. 10) y RF-03 (< 24 h para iniciar ⇒ máx. 5). Se
    // aplica siempre el límite más restrictivo entre los que correspondan.
    private static int? GetTransactionTicketLimit(TimeSpan timeUntilEventStart, decimal eventTicketPrice)
    {
        int? limit = null;

        if (timeUntilEventStart < TimeSpan.FromHours(24))
        {
            limit = 5;
        }

        if (eventTicketPrice > 100m)
        {
            limit = limit is null ? 10 : Math.Min(limit.Value, 10);
        }

        return limit;
    }

    private static void ValidateBuyerName(string buyerName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(buyerName))
        {
            errors.Add("El nombre del comprador es obligatorio.");
        }
    }

    private static void ValidateQuantity(int quantity, List<string> errors)
    {
        if (quantity < 1)
        {
            errors.Add("La cantidad debe ser 1 o más.");
        }
    }
}
