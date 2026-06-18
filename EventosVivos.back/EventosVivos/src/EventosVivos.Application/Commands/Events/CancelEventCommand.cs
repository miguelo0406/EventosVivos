using MediatR;

namespace EventosVivos.Application.Commands.Events;

// CQRS — Command: cancela un evento. No retorna payload (204 NoContent), por eso usa
// el marcador Unit de MediatR.
public sealed record CancelEventCommand(
    Guid EventId
) : IRequest<Unit>;