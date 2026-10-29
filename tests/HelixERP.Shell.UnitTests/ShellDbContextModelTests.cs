using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Platform;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Shell.UnitTests;

public sealed class ShellDbContextModelTests
{
    private const string UnusedConnectionString =
        "Server=localhost;Database=HelixERP_ModelTests;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;Connect Timeout=1";

    private static ShellDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShellDbContext>()
            .UseSqlServer(UnusedConnectionString)
            .Options;

        return new ShellDbContext(options);
    }

    [Fact]
    public void Context_uses_sql_server_provider()
    {
        using var context = CreateContext();

        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Fact]
    public void Module_registration_is_mapped_to_expected_table()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(ModuleRegistration));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ModuleRegistrations");
        entityType!.GetSchema().Should().BeNull();
    }

    [Fact]
    public void Module_registration_has_expected_keys_indexes_and_lengths()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ModuleRegistration))!;

        entityType.FindPrimaryKey().Should().NotBeNull();
        entityType.FindPrimaryKey()!.Properties.Select(property => property.Name).Should().Equal("Id");

        entityType.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(ModuleRegistration.ModuleKey));

        entityType.FindProperty(nameof(ModuleRegistration.ModuleKey))!.GetMaxLength().Should().Be(64);
        entityType.FindProperty(nameof(ModuleRegistration.DisplayName))!.GetMaxLength().Should().Be(200);
        entityType.FindProperty(nameof(ModuleRegistration.Host))!.GetMaxLength().Should().Be(255);
    }

    [Fact]
    public void Module_registration_has_row_version_concurrency_token()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ModuleRegistration))!;

        var rowVersion = entityType.FindProperty("RowVersion");

        rowVersion.Should().NotBeNull();
        rowVersion!.IsConcurrencyToken.Should().BeTrue();
        rowVersion.IsShadowProperty().Should().BeTrue();
    }

    [Fact]
    public void Domain_events_and_computed_base_url_are_not_mapped()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ModuleRegistration))!;

        entityType.GetProperties().Should().NotContain(property => property.Name == "DomainEvents");
        entityType.GetProperties().Should().NotContain(property => property.Name == nameof(ModuleRegistration.BaseUrl));
    }

    [Fact]
    public void Initial_migration_is_discovered()
    {
        using var context = CreateContext();

        var migrations = context.Database.GetMigrations().ToList();

        migrations.Should().ContainSingle(migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_script_contains_module_registrations_table()
    {
        using var context = CreateContext();

        var script = context.Database.GenerateCreateScript();

        script.Should().Contain("CREATE TABLE [ModuleRegistrations]");
        script.Should().Contain("[ModuleKey] nvarchar(64) NOT NULL");
        script.Should().Contain("[RowVersion] rowversion");
        script.Should().Contain("CREATE UNIQUE INDEX [IX_ModuleRegistrations_ModuleKey]");
    }
}