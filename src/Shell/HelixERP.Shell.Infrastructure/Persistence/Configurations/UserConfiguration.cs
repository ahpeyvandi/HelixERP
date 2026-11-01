using HelixERP.BuildingBlocks.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Login).HasMaxLength(64).IsRequired();
        builder.HasIndex(user => user.Login).IsUnique();

        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.IsSuperUser).IsRequired();
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.CompanyId);
        builder.Property(user => user.CreatedAtUtc).IsRequired();
        builder.Property(user => user.LastLoginAtUtc);

        builder.HasMany(user => user.Memberships)
            .WithOne()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(User.Memberships))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(user => user.ExternalLogins)
            .WithOne()
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(User.ExternalLogins))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();
    }
}