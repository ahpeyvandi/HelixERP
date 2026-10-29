using HelixERP.BuildingBlocks.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");

        builder.HasKey(group => group.Id);
        builder.Property(group => group.Id).ValueGeneratedNever();

        builder.Property(group => group.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(group => group.Name).IsUnique();

        builder.Property(group => group.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(group => group.Category).HasMaxLength(100).IsRequired();
        builder.Property(group => group.IsSystem).IsRequired();
        builder.Property(group => group.CreatedAtUtc).IsRequired();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(group => group.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(group => group.ParentId);

        builder.Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();
    }
}