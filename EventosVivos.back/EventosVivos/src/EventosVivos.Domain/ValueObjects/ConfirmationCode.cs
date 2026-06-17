namespace EventosVivos.Domain.ValueObjects;

// Value Object: encapsula el formato "EV-123456" del código de reserva (RF-04).
// Responsabilidad única (S de SOLID): solo sabe generar y representar el código; no
// conoce Reservation ni cómo se persiste o se garantiza su unicidad (eso es
// responsabilidad del caso de uso, que sí tiene acceso al repositorio).
public sealed class ConfirmationCode : IEquatable<ConfirmationCode>
{
    private const string Prefix = "EV-";
    private const int MaxCandidateValue = 1_000_000;

    private ConfirmationCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ConfirmationCode Generate(Random? randomGenerator = null)
    {
        var generator = randomGenerator ?? Random.Shared;
        var digits = generator.Next(minValue: 0, maxValue: MaxCandidateValue).ToString("D6");

        return new(Prefix + digits);
    }

    public static ConfirmationCode FromExisting(string value) => new(value);

    public bool Equals(ConfirmationCode? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as ConfirmationCode);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
