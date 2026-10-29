using System.Text.RegularExpressions;
using HelixERP.BuildingBlocks.Application.Storage;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.BuildingBlocks.Domain.Platform;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Application.Storage;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelixERP.Shell.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private static readonly Regex ExtensionPattern = new(
        @"^\.[A-Za-z0-9]{1,10}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ShellDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(
        ShellDbContext dbContext,
        IClock clock,
        ICurrentUser currentUser,
        IOptions<FileStorageOptions> options,
        ILogger<LocalFileStorage> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
        _options = options.Value;
        _logger = logger;
        _options.Validate();
    }

    private string RootPath => Path.GetFullPath(_options.RootPath);

    public async Task<FileMetadataDto> SaveAsync(
        FileUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        if (request.Length is { } declaredLength && declaredLength > _options.MaxFileSizeBytes)
        {
            throw new DomainException($"File exceeds the maximum allowed size of {_options.MaxFileSizeBytes} bytes.");
        }

        var fileName = FileAttachment.NormalizeFileName(request.FileName);
        var storageKey = BuildStorageKey(fileName);
        var fullPath = ResolvePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var temporaryPath = $"{fullPath}.uploading";
        long sizeBytes;

        try
        {
            sizeBytes = await WriteToTemporaryFileAsync(
                request.Content,
                temporaryPath,
                cancellationToken);

            if (sizeBytes == 0)
            {
                throw new DomainException("Empty files cannot be uploaded.");
            }

            File.Move(temporaryPath, fullPath, overwrite: false);
        }
        catch
        {
            TryDelete(temporaryPath);
            TryDelete(fullPath);
            throw;
        }

        var now = _clock.UtcNow;
        var attachment = FileAttachment.Create(
            fileName,
            storageKey,
            string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            sizeBytes,
            request.RelatedEntity,
            request.RelatedId,
            _currentUser.UserId,
            now);

        _dbContext.Files.Add(attachment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            TryDelete(fullPath);
            throw;
        }

        return ToDto(attachment);
    }

    public async Task<FileContent?> OpenAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty)
        {
            return null;
        }

        var attachment = await _dbContext.Files
            .AsNoTracking()
            .SingleOrDefaultAsync(file => file.Id == fileId && !file.IsDeleted, cancellationToken);

        if (attachment is null)
        {
            return null;
        }

        var fullPath = ResolvePath(attachment.StorageKey);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning(
                "File metadata {FileId} exists but the physical file is missing at {StorageKey}.",
                attachment.Id,
                attachment.StorageKey);

            return null;
        }

        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return new FileContent(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            stream);
    }

    public async Task<IReadOnlyList<FileMetadataDto>> ListAsync(
        string? relatedEntity = null,
        string? relatedId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Files.AsNoTracking().Where(file => !file.IsDeleted);

        if (!string.IsNullOrWhiteSpace(relatedEntity))
        {
            var entity = relatedEntity.Trim();
            query = query.Where(file => file.RelatedEntity == entity);
        }

        if (!string.IsNullOrWhiteSpace(relatedId))
        {
            var id = relatedId.Trim();
            query = query.Where(file => file.RelatedId == id);
        }

        var files = await query
            .OrderByDescending(file => file.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return files.Select(ToDto).ToList();
    }

    public async Task<bool> DeleteAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var attachment = await _dbContext.Files
            .SingleOrDefaultAsync(file => file.Id == fileId && !file.IsDeleted, cancellationToken);

        if (attachment is null)
        {
            return false;
        }

        attachment.MarkDeleted(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TryDelete(ResolvePath(attachment.StorageKey));

        return true;
    }

    private async Task<long> WriteToTemporaryFileAsync(
        Stream source,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        await using var target = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var buffer = new byte[81920];
        long total = 0;
        int read;

        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;

            if (total > _options.MaxFileSizeBytes)
            {
                throw new DomainException(
                    $"File exceeds the maximum allowed size of {_options.MaxFileSizeBytes} bytes.");
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        await target.FlushAsync(cancellationToken);

        return total;
    }

    private string BuildStorageKey(string fileName)
    {
        var now = _clock.UtcNow;
        var extension = Path.GetExtension(fileName);
        var safeExtension = ExtensionPattern.IsMatch(extension) ? extension.ToLowerInvariant() : string.Empty;

        return $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{safeExtension}";
    }

    private string ResolvePath(string storageKey)
    {
        var root = RootPath;
        var fullPath = Path.GetFullPath(
            Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));

        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("The resolved storage path escapes the configured file root.");
        }

        return fullPath;
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Could not delete the physical file {Path}.", path);
        }
    }

    private static FileMetadataDto ToDto(FileAttachment attachment)
        => new(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.RelatedEntity,
            attachment.RelatedId,
            attachment.UploadedByUserId,
            attachment.CreatedAtUtc);
}