using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.BuildingBlocks.Domain.Platform;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Application.Modules;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Shell.Infrastructure.Modules;

public sealed class ModuleRegistry : IModuleRegistry
{
    private readonly ShellDbContext _dbContext;
    private readonly IClock _clock;

    public ModuleRegistry(ShellDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ModuleRegistrationDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var modules = await _dbContext.ModuleRegistrations
            .AsNoTracking()
            .OrderBy(module => module.ModuleKey)
            .ToListAsync(cancellationToken);

        return modules.Select(ToDto).ToList();
    }

    public async Task<ModuleRegistrationDto?> FindAsync(string moduleKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeLookupKey(moduleKey);

        var module = await _dbContext.ModuleRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(registration => registration.ModuleKey == normalizedKey, cancellationToken);

        return module is null ? null : ToDto(module);
    }

    public async Task<ModuleRegistrationDto> RegisterAsync(
        RegisterModuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeLookupKey(request.ModuleKey);

        var exists = await _dbContext.ModuleRegistrations
            .AnyAsync(registration => registration.ModuleKey == normalizedKey, cancellationToken);

        if (exists)
        {
            throw new DomainException($"Module '{normalizedKey}' is already registered.");
        }

        var module = ModuleRegistration.Create(
            normalizedKey,
            request.DisplayName,
            request.Host,
            request.Port,
            _clock.UtcNow);

        _dbContext.ModuleRegistrations.Add(module);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(module);
    }

    public async Task<ModuleRegistrationDto> UpdateEndpointAsync(
        string moduleKey,
        UpdateModuleEndpointRequest request,
        CancellationToken cancellationToken = default)
    {
        var module = await GetTrackedAsync(moduleKey, cancellationToken);

        module.UpdateEndpoint(request.Host, request.Port, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(module);
    }

    public async Task<ModuleRegistrationDto> SetActiveAsync(
        string moduleKey,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var module = await GetTrackedAsync(moduleKey, cancellationToken);

        if (isActive)
        {
            module.Activate(_clock.UtcNow);
        }
        else
        {
            module.Deactivate(_clock.UtcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(module);
    }

    public async Task<bool> RemoveAsync(string moduleKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeLookupKey(moduleKey);

        var module = await _dbContext.ModuleRegistrations
            .SingleOrDefaultAsync(registration => registration.ModuleKey == normalizedKey, cancellationToken);

        if (module is null)
        {
            return false;
        }

        _dbContext.ModuleRegistrations.Remove(module);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<ModuleRegistration> GetTrackedAsync(string moduleKey, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeLookupKey(moduleKey);

        var module = await _dbContext.ModuleRegistrations
            .SingleOrDefaultAsync(registration => registration.ModuleKey == normalizedKey, cancellationToken);

        if (module is null)
        {
            throw new KeyNotFoundException($"Module '{normalizedKey}' is not registered.");
        }

        return module;
    }

    private static string NormalizeLookupKey(string moduleKey)
    {
        if (string.IsNullOrWhiteSpace(moduleKey))
        {
            throw new DomainException("Module key is required.");
        }

        return moduleKey.Trim().ToLowerInvariant();
    }

    private static ModuleRegistrationDto ToDto(ModuleRegistration module)
        => new(
            module.Id,
            module.ModuleKey,
            module.DisplayName,
            module.Host,
            module.Port,
            module.IsActive,
            module.BaseUrl,
            module.CreatedAtUtc,
            module.UpdatedAtUtc);
}