using EventosVivos.Domain.Entities;
using EventosVivos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(reservation => reservation.Id);

        builder.Property(reservation => reservation.Quantity).IsRequired();

        builder.Property(reservation => reservation.BuyerName)
            .IsRequired()
            .HasMaxLength(150);

        // Value Object Email: se persiste como una columna de texto simple mediante
        // una conversión explícita (Email <-> string). Domain sigue sin conocer EF.
        builder.Property(reservation => reservation.BuyerEmail)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("buyer_email")
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.Property(reservation => reservation.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(reservation => reservation.ConfirmationCode)
            .HasMaxLength(20)
            .HasColumnName("confirmation_code")
            .HasConversion(
                code => code!.Value,
                value => ConfirmationCode.FromExisting(value));

        // RF-04: el código de reserva debe ser único. PostgreSQL permite múltiples
        // NULL en una columna con índice único (reservas aún no confirmadas).
        builder.HasIndex(reservation => reservation.ConfirmationCode).IsUnique();

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(reservation => reservation.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => reservation.EventId);
    }
}
