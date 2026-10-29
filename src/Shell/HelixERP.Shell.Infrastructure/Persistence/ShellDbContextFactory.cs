using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HelixERP.Shell.Infrastructure.Persistence;

public sealed class ShellDbContextFactory : IDesignTimeDbContextFactory<ShellDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__HelixDb";

    private const string DefaultConnectionString =
        "Server=localhost;Database=HelixERP;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True";

    public ShellDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DefaultConnectionString;
        }

        var options = new DbContextOptionsBuilder<ShellDbContext>()
            .UseSqlServer(
                connectionString,
                sqlServer => sqlServer.MigrationsAssembly(typeof(ShellDbContext).Assembly.FullName))
            .Options;

        return new ShellDbContext(options);
    }
}