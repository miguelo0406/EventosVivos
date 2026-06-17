using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.ValueObjects;

// Value Object inmutable: garantiza que toda instancia contiene un email válido.
// Principio de responsabilidad única (S de SOLID): la única razón de cambio de esta
// clase es el formato de email, nada más.
public sealed partial class Email : IEquatable<Email>
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // Sigue la convención TryParse/Parse de .NET para permitir que el código
    // invocador decida si quiere agregar el error a una lista de validaciones
    // (TryCreate) o recibir una excepción inmediatamente (Create).
    public static bool TryCreate(string? value, [NotNullWhen(true)] out Email? email)
    {
        var normalizedValue = value?.Trim() ?? string.Empty;

        if (normalizedValue.Length == 0 || !EmailPattern().IsMatch(normalizedValue))
        {
            email = null;
            return false;
        }

        email = new(normalizedValue);
        return true;
    }

    public static Email Create(string value)
    {
        if (!TryCreate(value: value, email: out var email))
        {
            throw new InvalidEmailFormatException(invalidValue: value ?? string.Empty);
        }

        return email;
    }

    public bool Equals(Email? other) =>
        other is not null && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.ToLowerInvariant().GetHashCode();

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
