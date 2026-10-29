using HelixERP.Shell.Application.Modules;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace HelixERP.Shell.Api.Gateway;

public sealed class DatabaseProxyConfigProvider : IProxyConfigProvider, IModuleRouteSynchronizer, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseProxyConfigProvider> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    private ProxySnapshot _snapshot = ProxySnapshot.CreateEmpty();

    public DatabaseProxyConfigProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseProxyConfigProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public IProxyConfig GetConfig() => _snapshot;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            var modules = await registry.ListAsync(cancellationToken);

            var snapshot = new ProxySnapshot(
                ModuleRouteBuilder.BuildRoutes(modules),
                ModuleRouteBuilder.BuildClusters(modules),
                Guid.NewGuid().ToString("N"));

            var previous = Interlocked.Exchange(ref _snapshot, snapshot);
            previous.SignalChange();

            _logger.LogInformation(
                "Gateway routes reloaded: {ActiveModules} active module(s).",
                snapshot.Routes.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Failed to reload gateway routes from the module registry.");
            throw;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void Dispose()
    {
        _snapshot.Dispose();
        _reloadLock.Dispose();
    }

    private sealed class ProxySnapshot : IProxyConfig, IDisposable
    {
        private readonly CancellationTokenSource _changeSource = new();

        public static ProxySnapshot CreateEmpty() => new([], [], "empty");

        public ProxySnapshot(
            IReadOnlyList<RouteConfig> routes,
            IReadOnlyList<ClusterConfig> clusters,
            string revisionId)
        {
            Routes = routes;
            Clusters = clusters;
            RevisionId = revisionId;
        }

        public IReadOnlyList<RouteConfig> Routes { get; }

        public IReadOnlyList<ClusterConfig> Clusters { get; }

        public string RevisionId { get; }

        public IChangeToken ChangeToken => new CancellationChangeToken(_changeSource.Token);

        public void SignalChange()
        {
            if (!_changeSource.IsCancellationRequested)
            {
                _changeSource.Cancel();
            }
        }

        public void Dispose() => _changeSource.Dispose();
    }
}