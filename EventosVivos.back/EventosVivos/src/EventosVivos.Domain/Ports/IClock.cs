namespace EventosVivos.Domain.Ports;

// Puerto de salida: abstrae la obtención de la hora actual para que Application sea
// testeable de forma determinística, sin depender de DateTime.UtcNow real.
// Liskov Substitution (L de SOLID): cualquier implementación (SystemClock en
// producción, FixedClock en pruebas) es intercambiable sin alterar el comportamiento
// esperado por quien consume IClock.
public interface IClock
{
    DateTime UtcNow { get; }
}
