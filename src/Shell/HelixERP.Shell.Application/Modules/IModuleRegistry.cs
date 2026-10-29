namespace HelixERP.Shell.Application.Modules;

public interface IModuleRegistry
{
    Task<IReadOnlyList<ModuleRegistrationDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ModuleRegistrationDto?> FindAsync(string moduleKey, CancellationToken cancellationToken = default);

    Task<ModuleRegistrationDto> RegisterAsync(
        RegisterModuleRequest request,
        CancellationToken cancellationToken = default);

    Task<ModuleRegistrationDto> UpdateEndpointAsync(
        string moduleKey,
        UpdateModuleEndpointRequest request,
        CancellationToken cancellationToken = default);

    Task<ModuleRegistrationDto> SetActiveAsync(
        string moduleKey,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string moduleKey, CancellationToken cancellationToken = default);
}