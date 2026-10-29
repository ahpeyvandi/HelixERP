using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Shell.UnitTests;

internal static class TestDb
{
    public static ShellDbContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<ShellDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ShellDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}

internal sealed class TestClock : IClock
{
    public TestClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}

internal sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; }

    public Guid? UserId { get; set; }

    public Guid? CompanyId { get; set; }

    public string? UserName { get; set; }

    public string? DisplayName { get; set; }

    public bool IsSuperUser { get; set; }

    public static TestCurrentUser For(Guid userId, string login = "tester") => new()
    {
        IsAuthenticated = true,
        UserId = userId,
        UserName = login,
        DisplayName = login
    };

    public static TestCurrentUser Anonymous() => new();
}