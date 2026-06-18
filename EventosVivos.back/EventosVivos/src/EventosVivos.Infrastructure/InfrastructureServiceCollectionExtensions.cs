using EventosVivos.Application.Ports;
using EventosVivos.Domain.Ports;
using EventosVivos.Infrastructure.Identity;
using EventosVivos.Infrastructure.Persistence;
using EventosVivos.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Infrastructure;

// Composition root de Infrastructure: el único lugar donde se decide qué
// implementación concreta satisface cada puerto. Api solo invoca este método; no conoce
// EF Core, Npgsql ni Keycloak directamente (Inversión de dependencias, D de SOLID).
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(name: "EventosVivosDatabase")
            ?? throw new InvalidOperationException(message: "No se configuró la cadena de conexión 'EventosVivosDatabase'.");

        services.AddDbContext<EventosVivosDbContext>(optionsAction: optionsBuilder =>
            optionsBuilder.UseNpgsql(connectionString));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IClock, SystemClock>();

        // Keycloak como proveedor de identidad (adaptador del puerto IIdentityProvider).
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.AddHttpClient<IIdentityProvider, KeycloakIdentityProvider>();

        return services;
    }
}
