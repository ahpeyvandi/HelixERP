using FluentAssertions;
using HelixERP.Shell.Application.Bootstrap;
using HelixERP.Shell.Infrastructure.Bootstrap;
using HelixERP.Shell.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HelixERP.Shell.UnitTests;

public sealed class ShellDbInitializerTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    private static ShellDbInitializer CreateInitializer(
        Infrastructure.Persistence.ShellDbContext db,
        BootstrapOptions options)
        => new(
            db,
            new Pbkdf2PasswordHasher(iterations: 100_000),
            new TestClock(Now),
            Options.Create(options),
            NullLogger<ShellDbInitializer>.Instance);

    [Fact]
    public async Task Seeds_standard_groups_access_rights_and_admin()
    {
        await using var db = TestDb.Create();
        var options = new BootstrapOptions
        {
            AdminLogin = "admin",
            AdminEmail = "admin@helix.local",
            AdminPassword = "Admin#12345"
        };

        await CreateInitializer(db, options).SeedAsync(options);

        var groups = await db.Groups.ToListAsync();
        groups.Should().HaveCount(9);
        groups.Should().Contain(group => group.Name == "sales.group_sales_manager");
        groups.Single(group => group.Name == "sales.group_sales_manager").ParentId.Should().NotBeNull();

        (await db.AccessRights.CountAsync()).Should().Be(8);

        var admin = await db.Users.Include(user => user.Memberships).SingleAsync();
        admin.Login.Should().Be("admin");
        admin.IsSuperUser.Should().BeTrue();
        admin.Memberships.Should().HaveCount(3);
        new Pbkdf2PasswordHasher(iterations: 100_000)
            .Verify("Admin#12345", admin.PasswordHash)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Seeding_is_idempotent()
    {
        await using var db = TestDb.Create();
        var options = new BootstrapOptions { AdminPassword = "Admin#12345" };

        await CreateInitializer(db, options).SeedAsync(options);
        await CreateInitializer(db, options).SeedAsync(options);

        (await db.Groups.CountAsync()).Should().Be(9);
        (await db.AccessRights.CountAsync()).Should().Be(8);
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Missing_admin_password_skips_admin_creation()
    {
        await using var db = TestDb.Create();
        var options = new BootstrapOptions { AdminPassword = null };

        await CreateInitializer(db, options).SeedAsync(options);

        (await db.Users.CountAsync()).Should().Be(0);
        (await db.Groups.CountAsync()).Should().Be(9);
    }
}