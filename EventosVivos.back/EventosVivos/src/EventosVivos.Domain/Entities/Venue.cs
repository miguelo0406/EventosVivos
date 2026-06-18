using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Entities;

// Entidad de referencia (datos semilla: Auditorio Central, Sala Norte, Arena Sur). El
// enunciado los trata como preexistentes; el CRUD de organizador es valor agregado. Igual
// que Event, se construye por factory method y valida sus propias invariantes (nombre,
// capacidad, ciudad) lanzando DomainValidationException, de modo que nunca exista en
// memoria un Venue mal formado. Los guardas cruzados (no reducir el aforo por debajo de un
// evento ya programado, no borrar un venue con eventos) viven en la capa de aplicación,
// porque requieren consultar el agregado Event.
public sealed class Venue
{
    private const int MinNameLength = 3;
    private const int MaxNameLength = 150;
    private const int MaxCityLength = 100;

    private Venue()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Capacity { get; private set; }

    public string City { get; private set; } = string.Empty;

    public static Venue Create(
        int id,
        string name,
        int capacity,
        string city)
    {
        Validate(
            name: name,
            capacity: capacity,
            city: city
        );

        return new()
        {
            Id = id,
            Name = name.Trim(),
            Capacity = capacity,
            City = city.Trim(),
        };
    }

    // Actualiza los datos editables del venue. La capacidad solo valida aquí su invariante
    // propia (entero positivo); que no quede por debajo de un evento ya programado lo
    // comprueba el servicio de aplicación antes de llamar a este método.
    public void Update(string name, int capacity, string city)
    {
        Validate(name: name, capacity: capacity, city: city);

        Name = name.Trim();
        Capacity = capacity;
        City = city.Trim();
    }

    private static void Validate(string name, int capacity, string city)
    {
        List<string> validationErrors = [];

        var trimmedName = name?.Trim().Length ?? 0;
        if (trimmedName < MinNameLength || trimmedName > MaxNameLength)
        {
            validationErrors.Add($"El nombre del venue debe tener entre {MinNameLength} y {MaxNameLength} caracteres.");
        }

        if (capacity <= 0)
        {
            validationErrors.Add("La capacidad del venue debe ser un entero positivo.");
        }

        var trimmedCity = city?.Trim().Length ?? 0;
        if (trimmedCity is 0 or > MaxCityLength)
        {
            validationErrors.Add($"La ciudad es obligatoria y no puede superar {MaxCityLength} caracteres.");
        }

        if (validationErrors.Count > 0)
        {
            throw new DomainValidationException(errors: validationErrors);
        }
    }
}