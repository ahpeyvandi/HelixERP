namespace HelixERP.Shell.Application.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public const long DefaultMaxFileSizeBytes = 100L * 1024 * 1024;

    public string RootPath { get; set; } = "volumes/files";

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RootPath))
        {
            throw new InvalidOperationException("FileStorage:RootPath must be configured.");
        }

        if (MaxFileSizeBytes is < 1 or > 2L * 1024 * 1024 * 1024)
        {
            throw new InvalidOperationException(
                "FileStorage:MaxFileSizeBytes must be between 1 byte and 2 GiB.");
        }
    }
}