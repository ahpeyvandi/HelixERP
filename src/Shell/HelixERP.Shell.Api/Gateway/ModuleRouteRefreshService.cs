namespace HelixERP.Shell.Api.Gateway;

public sealed class ModuleRouteRefreshService : BackgroundService
{
    private readonly IModuleRouteSynchronizer _synchronizer;
    private readonly ILogger<ModuleRouteRefreshService> _logger;
    private readonly TimeSpan _interval;

    public ModuleRouteRefreshService(
        IModuleRouteSynchronizer synchronizer,
        IConfiguration configuration,
        ILogger<ModuleRouteRefreshService> logger)
    {
        _synchronizer = synchronizer;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(configuration.GetValue("Gateway:RefreshSeconds", 30));
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await ReloadSafelyAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_interval <= TimeSpan.Zero)
        {
            return;
        }

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ReloadSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ReloadSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _synchronizer.ReloadAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Gateway route refresh failed; the previous snapshot stays active.");
        }
    }
}