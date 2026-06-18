using EventosVivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence;

public sealed class EventosVivosDbContext : DbContext
{
    public EventosVivosDbContext(DbContextOptions<EventosVivosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventosVivosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
