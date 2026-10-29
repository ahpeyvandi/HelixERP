using System.Text;
using FluentAssertions;
using HelixERP.BuildingBlocks.Application.Storage;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.Shell.Application.Storage;
using HelixERP.Shell.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HelixERP.Shell.UnitTests;

public sealed class LocalFileStorageTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "helix-file-unit-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Saves_file_metadata_and_physical_content()
    {
        await using var db = TestDb.Create();
        var storage = CreateStorage(db);

        await using var source = new MemoryStream(Encoding.UTF8.GetBytes("hello helix"));
        var metadata = await storage.SaveAsync(new FileUploadRequest(
            source,
            "greeting.txt",
            "text/plain",
            source.Length,
            "sales.order",
            "42"));

        metadata.FileName.Should().Be("greeting.txt");
        metadata.SizeBytes.Should().Be(11);
        metadata.RelatedEntity.Should().Be("sales.order");
        metadata.UploadedByUserId.Should().NotBeNull();

        var files = Directory.GetFiles(_root, "*", SearchOption.AllDirectories);
        files.Should().ContainSingle();

        await using var download = await storage.OpenAsync(metadata.Id);
        download.Should().NotBeNull();
        download!.FileName.Should().Be("greeting.txt");

        using var reader = new StreamReader(download.Content, Encoding.UTF8);
        (await reader.ReadToEndAsync()).Should().Be("hello helix");
    }

    [Fact]
    public async Task Oversized_uploads_are_rejected_and_cleaned_up()
    {
        await using var db = TestDb.Create();
        var storage = CreateStorage(db, maxFileSizeBytes: 5);

        await using var declared = new MemoryStream(new byte[10]);
        var declaredAction = () => storage.SaveAsync(new FileUploadRequest(declared, "big.bin", "application/octet-stream", declared.Length));

        await declaredAction.Should().ThrowAsync<DomainException>();

        await using var streamed = new MemoryStream(new byte[10]);
        var streamedAction = () => storage.SaveAsync(new FileUploadRequest(streamed, "big.bin", "application/octet-stream"));

        await streamedAction.Should().ThrowAsync<DomainException>();

        if (Directory.Exists(_root))
        {
            Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }

        (await db.Files.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Empty_uploads_are_rejected()
    {
        await using var db = TestDb.Create();
        var storage = CreateStorage(db);

        await using var empty = new MemoryStream();
        var action = () => storage.SaveAsync(new FileUploadRequest(empty, "empty.txt", "text/plain", 0));

        await action.Should().ThrowAsync<DomainException>().WithMessage("*Empty*");
    }

    [Fact]
    public async Task Unsafe_file_names_are_rejected()
    {
        await using var db = TestDb.Create();
        var storage = CreateStorage(db);

        await using var source = new MemoryStream([1, 2, 3]);
        var action = () => storage.SaveAsync(new FileUploadRequest(source, "../escape.txt", "text/plain", 3));

        await action.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Delete_removes_the_physical_file_and_hides_metadata()
    {
        await using var db = TestDb.Create();
        var storage = CreateStorage(db);

        await using var source = new MemoryStream(Encoding.UTF8.GetBytes("delete me"));
        var metadata = await storage.SaveAsync(new FileUploadRequest(source, "temp.log", "text/plain", source.Length));

        (await storage.DeleteAsync(metadata.Id)).Should().BeTrue();
        (await storage.DeleteAsync(metadata.Id)).Should().BeFalse();

        Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
        (await storage.OpenAsync(metadata.Id)).Should().BeNull();
        (await storage.ListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Listing_filters_by_related_document()
    {
        await using var db = TestDb.Create();
        var storage = CreateStorage(db);

        await using var first = new MemoryStream(Encoding.UTF8.GetBytes("one"));
        await using var second = new MemoryStream(Encoding.UTF8.GetBytes("two"));

        await storage.SaveAsync(new FileUploadRequest(first, "one.txt", "text/plain", 3, "sales.order", "7"));
        await storage.SaveAsync(new FileUploadRequest(second, "two.txt", "text/plain", 3, "stock.move", "9"));

        var salesFiles = await storage.ListAsync("sales.order", "7");

        salesFiles.Should().ContainSingle();
        salesFiles[0].FileName.Should().Be("one.txt");
    }

    private LocalFileStorage CreateStorage(
        Infrastructure.Persistence.ShellDbContext db,
        long maxFileSizeBytes = FileStorageOptions.DefaultMaxFileSizeBytes)
        => new(
            db,
            new TestClock(Now),
            TestCurrentUser.For(Guid.NewGuid()),
            Options.Create(new FileStorageOptions
            {
                RootPath = _root,
                MaxFileSizeBytes = maxFileSizeBytes
            }),
            NullLogger<LocalFileStorage>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}