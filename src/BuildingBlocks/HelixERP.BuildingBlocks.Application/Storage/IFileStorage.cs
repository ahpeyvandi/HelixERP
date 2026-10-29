namespace HelixERP.BuildingBlocks.Application.Storage;

public sealed record FileUploadRequest(
    Stream Content,
    string FileName,
    string? ContentType = null,
    long? Length = null,
    string? RelatedEntity = null,
    string? RelatedId = null);

public sealed record FileMetadataDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? RelatedEntity,
    string? RelatedId,
    Guid? UploadedByUserId,
    DateTime CreatedAtUtc);

public sealed record FileContent(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public interface IFileStorage
{
    Task<FileMetadataDto> SaveAsync(
        FileUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<FileContent?> OpenAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileMetadataDto>> ListAsync(
        string? relatedEntity = null,
        string? relatedId = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);
}