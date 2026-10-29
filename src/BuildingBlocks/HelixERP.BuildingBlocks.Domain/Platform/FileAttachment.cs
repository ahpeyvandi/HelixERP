using System.Text.RegularExpressions;
using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Domain.Platform;

public sealed class FileAttachment : AggregateRoot<Guid>
{
    private static readonly Regex StorageKeyPattern = new(
        "^[A-Za-z0-9][A-Za-z0-9/_.-]{0,511}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private FileAttachment()
    {
    }

    private FileAttachment(
        Guid id,
        string fileName,
        string storageKey,
        string contentType,
        long sizeBytes,
        string? relatedEntity,
        string? relatedId,
        Guid? uploadedByUserId,
        DateTime createdAtUtc)
        : base(id)
    {
        FileName = fileName;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        RelatedEntity = relatedEntity;
        RelatedId = relatedId;
        UploadedByUserId = uploadedByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public string FileName { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = "application/octet-stream";

    public long SizeBytes { get; private set; }

    public string? RelatedEntity { get; private set; }

    public string? RelatedId { get; private set; }

    public Guid? UploadedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public static FileAttachment Create(
        string fileName,
        string storageKey,
        string contentType,
        long sizeBytes,
        string? relatedEntity = null,
        string? relatedId = null,
        Guid? uploadedByUserId = null,
        DateTime? createdAtUtc = null)
    {
        var createdAt = createdAtUtc ?? DateTime.UtcNow;
        EnsureUtc(createdAt, nameof(createdAtUtc));

        Guard.Against(sizeBytes < 0, "File size cannot be negative.");

        return new FileAttachment(
            Guid.NewGuid(),
            NormalizeFileName(fileName),
            NormalizeStorageKey(storageKey),
            NormalizeContentType(contentType),
            sizeBytes,
            NormalizeOptional(relatedEntity, 128),
            NormalizeOptional(relatedId, 128),
            uploadedByUserId,
            createdAt);
    }

    public void MarkDeleted(DateTime deletedAtUtc)
    {
        EnsureUtc(deletedAtUtc, nameof(deletedAtUtc));

        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
    }

    public static string NormalizeFileName(string fileName)
    {
        var value = Guard.NotNullOrWhiteSpace(fileName, nameof(fileName)).Trim();

        Guard.Against(value.Length > 255, "File name cannot exceed 255 characters.");
        Guard.Against(
            value.Contains('/') || value.Contains('\\') || value.Contains("..", StringComparison.Ordinal),
            "File name must not contain path separators or traversal sequences.");

        return value;
    }

    public static string NormalizeStorageKey(string storageKey)
    {
        var value = Guard.NotNullOrWhiteSpace(storageKey, nameof(storageKey)).Trim().Replace('\\', '/');

        Guard.Against(
            !StorageKeyPattern.IsMatch(value) || value.Contains("..", StringComparison.Ordinal),
            "Storage key must be a relative, traversal-free path.");

        return value;
    }

    public static string NormalizeContentType(string contentType)
    {
        var value = Guard.NotNullOrWhiteSpace(contentType, nameof(contentType)).Trim().ToLowerInvariant();

        Guard.Against(value.Length > 200, "Content type cannot exceed 200 characters.");

        return value;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        Guard.Against(trimmed.Length > maxLength, $"Value cannot exceed {maxLength} characters.");

        return trimmed;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(value.Kind != DateTimeKind.Utc, $"{parameterName} must be a UTC timestamp.");
    }
}