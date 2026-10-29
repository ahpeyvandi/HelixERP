using HelixERP.BuildingBlocks.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class ModuleRegistrationConfiguration : IEntityTypeConfiguration<ModuleRegistration>
{
    public void Configure(EntityTypeBuilder<ModuleRegistration> builder)
    {
        builder.ToTable("ModuleRegistrations");

        builder.HasKey(registration => registration.Id);
        builder.Property(registration => registration.Id).ValueGeneratedNever();

        builder.Property(registration => registration.ModuleKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(registration => registration.ModuleKey).IsUnique();

        builder.Property(registration => registration.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(registration => registration.Host)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(registration => registration.Port).IsRequired();
        builder.Property(registration => registration.IsActive).IsRequired();
        builder.Property(registration => registration.CreatedAtUtc).IsRequired();
        builder.Property(registration => registration.UpdatedAtUtc);

        builder.Ignore(registration => registration.BaseUrl);

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();
    }
}