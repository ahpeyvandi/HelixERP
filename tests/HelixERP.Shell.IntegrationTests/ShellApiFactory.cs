using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.Shell.Api.Gateway;
using HelixERP.Shell.Application.Storage;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Forwarder;

namespace HelixERP.Shell.IntegrationTests;

public sealed class ShellApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"helix-shell-tests-{Guid.NewGuid():N}";
    private readonly string _filesRoot = Path.Combine(
        Path.GetTempPath(),
        "helix-file-integration-tests",
        Guid.NewGuid().ToString("N"));
    private readonly TestServer? _upstream;

    public ShellApiFactory(bool useFakeUpstream = false)
    {
        if (useFakeUpstream)
        {
            _upstream = new TestServer(new WebHostBuilder().Configure(app =>
            {
                app.Run(async context =>
                {
                    LastUpstreamPath = context.Request.Path.Value;
                    LastUpstreamQuery = context.Request.QueryString.Value;
                    LastUpstreamUserId = context.Request.Headers[GatewayPolicies.UserIdHeader].ToString();
                    LastUpstreamGroups = context.Request.Headers[GatewayPolicies.GroupsHeader].ToString();
                    LastUpstreamSuperUser = context.Request.Headers[GatewayPolicies.SuperUserHeader].ToString();

                    await context.Response.WriteAsync("upstream-ok");
                });
            }));
        }
    }

    public string? LastUpstreamPath { get; private set; }

    public string? LastUpstreamQuery { get; private set; }

    public string? LastUpstreamUserId { get; private set; }

    public string? LastUpstreamGroups { get; private set; }

    public string? LastUpstreamSuperUser { get; private set; }

    public string FilesRoot => _filesRoot;

    public string AdminLogin => ConfigurationRoot["Bootstrap:AdminLogin"] ?? "admin";

    public string AdminPassword => ConfigurationRoot["Bootstrap:AdminPassword"] ?? "Helix@12345";

    private IConfiguration ConfigurationRoot => Services.GetRequiredService<IConfiguration>();

    public async Task<Guid> CreateInternalUserAsync(string login, string password)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShellDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<HelixERP.BuildingBlocks.Shared.Abstractions.IClock>();

        var group = await dbContext.Groups.SingleAsync(candidate => candidate.Name == "base.group_user");
        var user = User.Create(
            login,
            $"{login}@helix.local",
            login,
            passwordHasher.Hash(password),
            createdAtUtc: clock.UtcNow);

        user.AssignToGroup(group.Id, clock.UtcNow);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user.Id;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            var databaseDescriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(ShellDbContext) ||
                    descriptor.ServiceType == typeof(DbContextOptions<ShellDbContext>) ||
                    descriptor.ServiceType == typeof(DbContextOptions) ||
                    (descriptor.ServiceType.IsGenericType &&
                     descriptor.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1"))
                .ToList();

            foreach (var descriptor in databaseDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ShellDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.Configure<FileStorageOptions>(options => options.RootPath = _filesRoot);

            if (_upstream is not null)
            {
                services.Replace(ServiceDescriptor.Singleton<IForwarderHttpClientFactory>(
                    new UpstreamForwarderFactory(_upstream.CreateHandler())));
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _upstream?.Dispose();

            var tempRoot = Path.GetFullPath(Path.GetTempPath());
            var filesRoot = Path.GetFullPath(_filesRoot);

            if (filesRoot.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(filesRoot))
            {
                Directory.Delete(filesRoot, recursive: true);
            }
        }
    }

    private sealed class UpstreamForwarderFactory : IForwarderHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public UpstreamForwarderFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
            => new(_handler, disposeHandler: false);
    }
}