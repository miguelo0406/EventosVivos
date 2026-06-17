using EventosVivos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(eventEntity => eventEntity.Id);

        builder.Property(eventEntity => eventEntity.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(eventEntity => eventEntity.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(eventEntity => eventEntity.TicketPrice)
            .IsRequired()
            .HasPrecision(precision: 10, scale: 2);

        // Se almacena el nombre del enum como texto legible en vez de un entero: más
        // fácil de inspeccionar directamente en la base de datos durante soporte/QA.
        builder.Property(eventEntity => eventEntity.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(eventEntity => eventEntity.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(eventEntity => eventEntity.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(eventEntity => eventEntity.VenueId);
        builder.HasIndex(eventEntity => eventEntity.StartDateTime);
    }
}
