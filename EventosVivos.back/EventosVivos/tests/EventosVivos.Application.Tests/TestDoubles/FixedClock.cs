using EventosVivos.Domain.Ports;

namespace EventosVivos.Application.Tests.TestDoubles;

// Doble de prueba: implementa el mismo puerto IClock que SystemClock (Infrastructure)
// pero con un valor fijo, para que los tests sean deterministas. Sustitución de
// Liskov (L de SOLID): los casos de uso no distinguen entre este reloj y el real.
public sealed class FixedClock : IClock
{
    public FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}
