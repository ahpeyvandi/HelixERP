using HelixERP.BuildingBlocks.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class RecordRuleConfiguration : IEntityTypeConfiguration<RecordRule>
{
    public void Configure(EntityTypeBuilder<RecordRule> builder)
    {
        builder.ToTable("RecordRules");

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();

        builder.Property(rule => rule.Name).HasMaxLength(200).IsRequired();
        builder.Property(rule => rule.Model).HasMaxLength(128).IsRequired();
        builder.Property(rule => rule.Domain).HasMaxLength(4000).IsRequired();
        builder.Property(rule => rule.GroupId);
        builder.Property(rule => rule.Permissions).HasConversion<int>().IsRequired();
        builder.Property(rule => rule.IsActive).IsRequired();
        builder.Property(rule => rule.CompanyId);
        builder.Property(rule => rule.CreatedAtUtc).IsRequired();
        builder.Property(rule => rule.UpdatedAtUtc);

        builder.Ignore(rule => rule.IsGlobal);

        builder.HasIndex(rule => new { rule.Model, rule.IsActive });

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(rule => rule.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();
    }
}