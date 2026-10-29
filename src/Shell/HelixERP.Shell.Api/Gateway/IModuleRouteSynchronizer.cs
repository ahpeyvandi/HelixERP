namespace HelixERP.Shell.Api.Gateway;

public interface IModuleRouteSynchronizer
{
    Task ReloadAsync(CancellationToken cancellationToken = default);
}