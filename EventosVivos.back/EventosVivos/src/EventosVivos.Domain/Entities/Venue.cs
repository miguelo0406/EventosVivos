namespace EventosVivos.Domain.Entities;

// Entidad de referencia (datos semilla: Auditorio Central, Sala Norte, Arena Sur). No
// existe RF para crear venues vía API; se cargan mediante Infrastructure (seed).
public sealed class Venue
{
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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(message: "El nombre del venue es obligatorio.", paramName: nameof(name));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(capacity), message: "La capacidad debe ser un entero positivo.");
        }

        return new()
        {
            Id = id,
            Name = name,
            Capacity = capacity,
            City = city,
        };
    }
}
