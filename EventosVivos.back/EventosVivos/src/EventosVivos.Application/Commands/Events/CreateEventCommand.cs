using EventosVivos.Application.Dtos;
using MediatR;

namespace EventosVivos.Application.Commands.Events;

// CQRS — Command (RF-01): representa la intención de crear un evento. Es público
// porque el adaptador de entrada (controller) lo construye y lo envía vía IMediator.
public sealed record CreateEventCommand(
    CreateEventRequest Request
) : IRequest<EventResponse>;