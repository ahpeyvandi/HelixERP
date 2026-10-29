using HelixERP.BuildingBlocks.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("UserGroups");

        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).ValueGeneratedNever();

        builder.Property(membership => membership.UserId).IsRequired();
        builder.Property(membership => membership.GroupId).IsRequired();
        builder.Property(membership => membership.AssignedAtUtc).IsRequired();

        builder.HasIndex(membership => new { membership.UserId, membership.GroupId }).IsUnique();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(membership => membership.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}