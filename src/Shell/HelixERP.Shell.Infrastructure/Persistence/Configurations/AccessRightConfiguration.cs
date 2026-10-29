using HelixERP.BuildingBlocks.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class AccessRightConfiguration : IEntityTypeConfiguration<AccessRight>
{
    public void Configure(EntityTypeBuilder<AccessRight> builder)
    {
        builder.ToTable("AccessRights");

        builder.HasKey(right => right.Id);
        builder.Property(right => right.Id).ValueGeneratedNever();

        builder.Property(right => right.GroupId).IsRequired();
        builder.Property(right => right.Model).HasMaxLength(128).IsRequired();
        builder.Property(right => right.ModuleKey).HasMaxLength(64);
        builder.Property(right => right.Description).HasMaxLength(500);
        builder.Property(right => right.Rights).HasConversion<int>().IsRequired();
        builder.Property(right => right.CreatedAtUtc).IsRequired();
        builder.Property(right => right.UpdatedAtUtc);

        builder.HasIndex(right => new { right.GroupId, right.Model }).IsUnique();
        builder.HasIndex(right => right.Model);

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(right => right.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();
    }
}