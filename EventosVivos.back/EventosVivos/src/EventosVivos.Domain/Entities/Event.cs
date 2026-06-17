using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Entities;

// Entidad raíz de agregado. El constructor es privado y la única forma de crear un
// evento válido es a través de Create (Factory Method): así se garantiza que nunca
// exista en memoria un Event que viole sus propias reglas (invariante de diseño por
// construcción). Los setters son privados (encapsulamiento): el estado solo cambia a
// través de métodos explícitos del propio dominio (Cancel), nunca desde afuera.
public sealed class Event
{
    private const int MinTitleLength = 5;
    private const int MaxTitleLength = 100;
    private const int MinDescriptionLength = 10;
    private const int MaxDescriptionLength = 500;
    private const int WeekendLatestStartHour = 22;

    private Event()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int VenueId { get; private set; }

    public int MaxCapacity { get; private set; }

    public DateTime StartDateTime { get; private set; }

    public DateTime EndDateTime { get; private set; }

    public decimal TicketPrice { get; private set; }

    public EventType Type { get; private set; }

    public EventStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Event Create(
        string title,
        string description,
        int venueId,
        int venueCapacity,
        int maxCapacity,
        DateTime startDateTime,
        DateTime endDateTime,
        decimal ticketPrice,
        EventType type,
        DateTime currentTime)
    {
        List<string> validationErrors = [];

        ValidateTitle(title: title, errors: validationErrors);
        ValidateDescription(description: description, errors: validationErrors);
        ValidateMaxCapacity(maxCapacity: maxCapacity, errors: validationErrors);
        ValidateSchedule(
            startDateTime: startDateTime,
            endDateTime: endDateTime,
            currentTime: currentTime,
            errors: validationErrors);
        ValidatePrice(ticketPrice: ticketPrice, errors: validationErrors);

        if (validationErrors.Count > 0)
        {
            throw new DomainValidationException(errors: validationErrors);
        }

        // RN-01: el evento no puede exceder la capacidad del venue asignado.
        if (maxCapacity > venueCapacity)
        {
            throw new VenueCapacityExceededException(requestedCapacity: maxCapacity, venueCapacity: venueCapacity);
        }

        // RN-03: eventos en fin de semana no pueden iniciar después de las 22:00.
        var isWeekendNight = startDateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            && startDateTime.Hour >= WeekendLatestStartHour;
        if (isWeekendNight)
        {
            throw new WeekendNightRestrictionException(startDateTime: startDateTime);
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            VenueId = venueId,
            MaxCapacity = maxCapacity,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            TicketPrice = ticketPrice,
            Type = type,
            Status = EventStatus.Active,
            CreatedAt = currentTime,
        };
    }

    // RN-06: el estado "completado" es derivado, no persistido. Se calcula en el
    // momento de la consulta para no requerir un job en segundo plano.
    public EventStatus GetEffectiveStatus(DateTime currentTime)
    {
        if (Status == EventStatus.Active && currentTime > EndDateTime)
        {
            return EventStatus.Completed;
        }

        return Status;
    }

    public TimeSpan GetTimeUntilStart(DateTime currentTime) => StartDateTime - currentTime;

    public void Cancel(DateTime currentTime)
    {
        var effectiveStatus = GetEffectiveStatus(currentTime: currentTime);

        if (effectiveStatus == EventStatus.Cancelled)
        {
            throw new InvalidEventStateException(message: "El evento ya se encuentra cancelado.");
        }

        if (effectiveStatus == EventStatus.Completed)
        {
            throw new InvalidEventStateException(message: "No se puede cancelar un evento ya completado.");
        }

        Status = EventStatus.Cancelled;
    }

    private static void ValidateTitle(string title, List<string> errors)
    {
        var trimmedLength = title?.Trim().Length ?? 0;
        if (trimmedLength < MinTitleLength || trimmedLength > MaxTitleLength)
        {
            errors.Add($"El título debe tener entre {MinTitleLength} y {MaxTitleLength} caracteres.");
        }
    }

    private static void ValidateDescription(string description, List<string> errors)
    {
        var trimmedLength = description?.Trim().Length ?? 0;
        if (trimmedLength < MinDescriptionLength || trimmedLength > MaxDescriptionLength)
        {
            errors.Add($"La descripción debe tener entre {MinDescriptionLength} y {MaxDescriptionLength} caracteres.");
        }
    }

    private static void ValidateMaxCapacity(int maxCapacity, List<string> errors)
    {
        if (maxCapacity <= 0)
        {
            errors.Add("La capacidad máxima debe ser un entero positivo.");
        }
    }

    private static void ValidateSchedule(
        DateTime startDateTime,
        DateTime endDateTime,
        DateTime currentTime,
        List<string> errors)
    {
        if (startDateTime <= currentTime)
        {
            errors.Add("La fecha y hora de inicio debe ser futura.");
        }

        if (endDateTime <= startDateTime)
        {
            errors.Add("La fecha y hora de fin debe ser posterior al inicio.");
        }
    }

    private static void ValidatePrice(decimal ticketPrice, List<string> errors)
    {
        if (ticketPrice <= 0)
        {
            errors.Add("El precio de entrada debe ser un decimal positivo.");
        }
    }
}
