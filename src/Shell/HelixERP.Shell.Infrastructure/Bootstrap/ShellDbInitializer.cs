using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Application.Bootstrap;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelixERP.Shell.Infrastructure.Bootstrap;

public sealed class ShellDbInitializer
{
    private static readonly StandardGroup[] StandardGroups =
    [
        new("base.group_user", "Internal User", "Administration", null, false),
        new("base.group_system", "Settings", "Administration", "base.group_user", true),
        new("base.group_erp_manager", "Access Rights", "Administration", "base.group_user", true),
        new("sales.group_sales_user", "User", "Sales", "base.group_user", false),
        new("sales.group_sales_manager", "Manager", "Sales", "sales.group_sales_user", false),
        new("stock.group_stock_user", "User", "Inventory", "base.group_user", false),
        new("stock.group_stock_manager", "Manager", "Inventory", "stock.group_stock_user", false),
        new("account.group_account_user", "Accountant", "Accounting", "base.group_user", false),
        new("account.group_account_manager", "Adviser", "Accounting", "account.group_account_user", false)
    ];

    private static readonly StandardAccessRight[] StandardAccessRights =
    [
        new("base.group_system", "base.module_registration", AccessRightFlags.All),
        new("base.group_erp_manager", "base.module_registration", AccessRightFlags.All),
        new("base.group_user", "base.module_registration", AccessRightFlags.Read),
        new("base.group_erp_manager", "base.group", AccessRightFlags.All),
        new("base.group_erp_manager", "base.access_right", AccessRightFlags.All),
        new("base.group_erp_manager", "base.record_rule", AccessRightFlags.All),
        new("base.group_erp_manager", "base.user", AccessRightFlags.All),
        new("base.group_user", "base.attachment", AccessRightFlags.All)
    ];

    private readonly ShellDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly IOptions<BootstrapOptions> _options;
    private readonly ILogger<ShellDbInitializer> _logger;

    public ShellDbInitializer(
        ShellDbContext dbContext,
        IPasswordHasher passwordHasher,
        IClock clock,
        IOptions<BootstrapOptions> options,
        ILogger<ShellDbInitializer> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (options.MigrateDatabase && _dbContext.Database.IsRelational())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }

        if (!options.SeedOnStartup)
        {
            return;
        }

        await SeedAsync(options, cancellationToken);
    }

    public async Task SeedAsync(BootstrapOptions options, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var existingGroups = await _dbContext.Groups
            .ToDictionaryAsync(group => group.Name, StringComparer.Ordinal, cancellationToken);

        foreach (var definition in StandardGroups.Where(group => group.ParentName is null))
        {
            if (!existingGroups.ContainsKey(definition.Name))
            {
                var group = Group.Create(
                    definition.Name,
                    definition.DisplayName,
                    definition.Category,
                    isSystem: definition.IsSystem,
                    createdAtUtc: now);

                _dbContext.Groups.Add(group);
                existingGroups[definition.Name] = group;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var definition in StandardGroups.Where(group => group.ParentName is not null))
        {
            if (existingGroups.ContainsKey(definition.Name))
            {
                continue;
            }

            var parentId = existingGroups.TryGetValue(definition.ParentName!, out var parent) ? parent.Id : (Guid?)null;
            var group = Group.Create(
                definition.Name,
                definition.DisplayName,
                definition.Category,
                parentId,
                definition.IsSystem,
                now);

            _dbContext.Groups.Add(group);
            existingGroups[definition.Name] = group;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var existingRights = await _dbContext.AccessRights
            .Select(right => new { right.GroupId, right.Model })
            .ToListAsync(cancellationToken);
        var rightKeys = existingRights
            .Select(right => (right.GroupId, right.Model))
            .ToHashSet();

        foreach (var definition in StandardAccessRights)
        {
            if (!existingGroups.TryGetValue(definition.GroupName, out var group))
            {
                continue;
            }

            if (!rightKeys.Add((group.Id, definition.Model)))
            {
                continue;
            }

            _dbContext.AccessRights.Add(AccessRight.Create(
                group.Id,
                definition.Model,
                definition.Rights,
                moduleKey: definition.Model.Split('.')[0],
                description: $"Seeded by shell bootstrap for {definition.Model}.",
                createdAtUtc: now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await EnsureAdminAsync(options, existingGroups, now, cancellationToken);
    }

    private async Task EnsureAdminAsync(
        BootstrapOptions options,
        IReadOnlyDictionary<string, Group> groups,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var adminLogin = User.NormalizeLogin(options.AdminLogin);

        var admin = await _dbContext.Users
            .Include(user => user.Memberships)
            .SingleOrDefaultAsync(user => user.Login == adminLogin, cancellationToken);

        if (admin is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            _logger.LogWarning(
                "Bootstrap:AdminPassword is not configured; the administrative user '{AdminLogin}' was not created.",
                adminLogin);

            return;
        }

        admin = User.Create(
            adminLogin,
            options.AdminEmail,
            options.AdminDisplayName,
            _passwordHasher.Hash(options.AdminPassword),
            isSuperUser: true,
            createdAtUtc: now);

        foreach (var groupName in new[] { "base.group_user", "base.group_erp_manager", "base.group_system" })
        {
            if (groups.TryGetValue(groupName, out var group))
            {
                admin.AssignToGroup(group.Id, now);
            }
        }

        _dbContext.Users.Add(admin);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created the administrative user '{AdminLogin}'.", adminLogin);
    }

    private sealed record StandardGroup(
        string Name,
        string DisplayName,
        string Category,
        string? ParentName,
        bool IsSystem);

    private sealed record StandardAccessRight(
        string GroupName,
        string Model,
        AccessRightFlags Rights);
}