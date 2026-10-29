using HelixERP.BuildingBlocks.Application.Events;
using HelixERP.BuildingBlocks.Domain.Platform;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Shell.Infrastructure.Persistence;

public sealed class ShellDbContext : HelixDbContext
{
    public ShellDbContext(DbContextOptions<ShellDbContext> options)
        : base(options)
    {
    }

    public ShellDbContext(
        DbContextOptions<ShellDbContext> options,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, domainEventDispatcher)
    {
    }

    public DbSet<ModuleRegistration> ModuleRegistrations => Set<ModuleRegistration>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    public DbSet<AccessRight> AccessRights => Set<AccessRight>();

    public DbSet<RecordRule> RecordRules => Set<RecordRule>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<FileAttachment> Files => Set<FileAttachment>();
}