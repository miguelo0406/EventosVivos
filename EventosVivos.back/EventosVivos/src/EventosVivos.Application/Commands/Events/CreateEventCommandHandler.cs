using EventosVivos.Application.Dtos;
using EventosVivos.Application.Services.Interfaces;
using MediatR;

namespace EventosVivos.Application.Commands.Events;

// Handler delgado: no contiene lógica de negocio, solo delega en el servicio/facade
// (igual que en Contabot). Es internal: MediatR lo descubre por escaneo de ensamblado,
// pero no se expone fuera de Application.
internal sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventResponse>
{
    private readonly IEventService _eventService;

    public CreateEventCommandHandler(IEventService eventService)
    {
        _eventService = eventService;
    }

    public Task<EventResponse> Handle(
        CreateEventCommand request,
        CancellationToken cancellationToken
    ) =>
        _eventService.CreateAsync(
            request: request.Request,
            cancellationToken: cancellationToken
        );
}