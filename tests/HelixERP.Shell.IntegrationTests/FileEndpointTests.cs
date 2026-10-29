using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using HelixERP.BuildingBlocks.Application.Storage;

namespace HelixERP.Shell.IntegrationTests;

public sealed class FileEndpointTests
{
    [Fact]
    public async Task Upload_download_list_and_delete_round_trip()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);
        client.WithBearerToken(auth.Tokens.AccessToken);

        using var form = new MultipartFormDataContent();
        var filePart = new ByteArrayContent(Encoding.UTF8.GetBytes("hello helix"));
        filePart.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(filePart, "file", "hello.txt");
        form.Add(new StringContent("sales.order"), "relatedEntity");
        form.Add(new StringContent("42"), "relatedId");

        var upload = await client.PostAsync("/api/files", form);
        upload.StatusCode.Should().Be(HttpStatusCode.Created);

        var metadata = await upload.Content.ReadFromJsonAsync<FileMetadataDto>();
        metadata.Should().NotBeNull();
        metadata!.FileName.Should().Be("hello.txt");
        metadata.SizeBytes.Should().Be(11);
        metadata.RelatedEntity.Should().Be("sales.order");

        Directory.GetFiles(factory.FilesRoot, "*", SearchOption.AllDirectories)
            .Should().ContainSingle("files must be stored in the configured erp_files root");

        var download = await client.GetAsync($"/api/files/{metadata.Id}");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        (await download.Content.ReadAsStringAsync()).Should().Be("hello helix");

        var list = await client.GetFromJsonAsync<List<FileMetadataDto>>(
            "/api/files?relatedEntity=sales.order&relatedId=42");
        list.Should().ContainSingle(entry => entry.Id == metadata.Id);

        var delete = await client.DeleteAsync($"/api/files/{metadata.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.GetAsync($"/api/files/{metadata.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        Directory.GetFiles(factory.FilesRoot, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task Anonymous_uploads_are_rejected()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([1, 2, 3]), "file", "a.bin");

        var response = await client.PostAsync("/api/files", form);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Missing_file_returns_not_found()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);
        client.WithBearerToken(auth.Tokens.AccessToken);

        var response = await client.GetAsync($"/api/files/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}