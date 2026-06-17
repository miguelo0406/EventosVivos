using EventosVivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

// Adaptador de salida: traduce la entidad de Domain al modelo relacional. Domain no
// tiene ninguna referencia a EF Core (ni atributos, ni dependencias): toda la
// configuración de mapeo vive aquí, en Infrastructure.
public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(venue => venue.Id);

        builder.Property(venue => venue.Id).ValueGeneratedNever();

        builder.Property(venue => venue.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(venue => venue.Capacity).IsRequired();

        builder.Property(venue => venue.City)
            .IsRequired()
            .HasMaxLength(100);

        // Datos de referencia fijos del enunciado de la prueba.
        builder.HasData(
            Venue.Create(id: 1, name: "Auditorio Central", capacity: 200, city: "Bogotá"),
            Venue.Create(id: 2, name: "Sala Norte", capacity: 50, city: "Bogotá"),
            Venue.Create(id: 3, name: "Arena Sur", capacity: 500, city: "Medellín"));
    }
}
