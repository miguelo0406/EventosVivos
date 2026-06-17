using EventosVivos.Domain.Ports;

namespace EventosVivos.Infrastructure;

// Adaptador de salida que implementa IClock. Responsabilidad única: traduce el
// puerto a la API real de .NET (DateTime.UtcNow). En pruebas se reemplaza por un
// FixedClock que no depende del reloj real (ver EventosVivos.Application.Tests).
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
