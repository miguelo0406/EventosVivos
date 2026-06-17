using EventosVivos.Application.Ports;
using EventosVivos.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Application;

// Composition root de Application: registra cada caso de uso detrás de su puerto de
// entrada. Interface Segregation (I de SOLID): Api recibe exactamente el caso de uso
// que necesita en cada controller, no una fachada gigante con todas las operaciones.
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationUseCases(this IServiceCollection services)
    {
        services.AddScoped<ICreateEventUseCase, CreateEventUseCase>();
        services.AddScoped<IListEventsUseCase, ListEventsUseCase>();
        services.AddScoped<IGetEventByIdUseCase, GetEventByIdUseCase>();
        services.AddScoped<ICancelEventUseCase, CancelEventUseCase>();
        services.AddScoped<IReserveTicketsUseCase, ReserveTicketsUseCase>();
        services.AddScoped<IConfirmReservationPaymentUseCase, ConfirmReservationPaymentUseCase>();
        services.AddScoped<ICancelReservationUseCase, CancelReservationUseCase>();
        services.AddScoped<IGetOccupancyReportUseCase, GetOccupancyReportUseCase>();
        services.AddScoped<IListVenuesUseCase, ListVenuesUseCase>();
        services.AddScoped<IGetReservationByIdUseCase, GetReservationByIdUseCase>();
        services.AddScoped<IListReservationsByEventUseCase, ListReservationsByEventUseCase>();

        return services;
    }
}
