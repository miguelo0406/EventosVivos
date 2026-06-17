namespace EventosVivos.Application.Ports;

// Puerto de entrada de soporte: el enunciado no define un RF explícito para cancelar
// un evento, pero el estado "cancelado" es parte del modelo (RF-02, RF-06) y debe ser
// alcanzable de algún modo. Ver CLAUDE.md, sección de decisiones de interpretación.
public interface ICancelEventUseCase
{
    Task ExecuteAsync(Guid eventId, CancellationToken cancellationToken);
}
