using HelixERP.BuildingBlocks.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixERP.Shell.Infrastructure.Persistence.Configurations;

public sealed class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> builder)
    {
        builder.ToTable("Files");

        builder.HasKey(file => file.Id);
        builder.Property(file => file.Id).ValueGeneratedNever();

        builder.Property(file => file.FileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.StorageKey).HasMaxLength(512).IsRequired();
        builder.HasIndex(file => file.StorageKey).IsUnique();

        builder.Property(file => file.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(file => file.SizeBytes).IsRequired();
        builder.Property(file => file.RelatedEntity).HasMaxLength(128);
        builder.Property(file => file.RelatedId).HasMaxLength(128);
        builder.Property(file => file.UploadedByUserId);
        builder.Property(file => file.CreatedAtUtc).IsRequired();
        builder.Property(file => file.IsDeleted).IsRequired();
        builder.Property(file => file.DeletedAtUtc);

        builder.HasIndex(file => new { file.RelatedEntity, file.RelatedId });
        builder.HasIndex(file => file.UploadedByUserId);

        builder.Property<byte[]>("RowVersion").IsRowVersion().IsConcurrencyToken();
    }
}