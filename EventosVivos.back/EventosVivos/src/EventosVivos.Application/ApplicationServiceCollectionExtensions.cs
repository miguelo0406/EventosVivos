using System.Reflection;
using EventosVivos.Application.Services;
using EventosVivos.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Application;

// Composition root de Application. Registra MediatR (que descubre Commands, Queries y
// sus Handlers por escaneo del ensamblado) y los servicios/facades detrás de su
// interfaz. Interface Segregation (I de SOLID): cada handler recibe solo el servicio
// que necesita, no una fachada monolítica.
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(applicationAssembly));

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
