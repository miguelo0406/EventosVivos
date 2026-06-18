using EventosVivos.Domain.Entities;
using EventosVivos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        // Value Object Email persistido como texto (conversión Email <-> string).
        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("email")
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.Property(user => user.KeycloakSubjectId)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("keycloak_subject_id");

        builder.Property(user => user.CreatedAt).IsRequired();

        // El subject de Keycloak es único: un usuario de Keycloak ↔ un usuario local.
        builder.HasIndex(user => user.KeycloakSubjectId).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
