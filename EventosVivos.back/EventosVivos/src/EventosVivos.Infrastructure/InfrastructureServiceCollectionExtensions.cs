using EventosVivos.Domain.Ports;
using EventosVivos.Infrastructure.Persistence;
using EventosVivos.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Infrastructure;

// Composition root de Infrastructure: el único lugar donde se decide qué
// implementación concreta satisface cada puerto de Domain. Api solo invoca este
// método; no conoce EF Core ni Npgsql directamente (Inversión de dependencias, D de
// SOLID, aplicada también en el cableado de servicios).
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EventosVivosDbContext>(optionsAction: optionsBuilder =>
            optionsBuilder.UseNpgsql(connectionString));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
