using HelixERP.BuildingBlocks.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");

        builder.HasKey(login => login.Id);
        builder.Property(login => login.Id).ValueGeneratedNever();

        builder.Property(login => login.UserId).IsRequired();
        builder.Property(login => login.Provider).HasMaxLength(128).IsRequired();
        builder.Property(login => login.Subject).HasMaxLength(256).IsRequired();
        builder.Property(login => login.CreatedAtUtc).IsRequired();

        builder.HasIndex(login => new { login.Provider, login.Subject })
            .IsUnique();
        builder.HasIndex(login => login.UserId);
    }
}
