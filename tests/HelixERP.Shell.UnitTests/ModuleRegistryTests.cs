using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.Shell.Application.Modules;
using HelixERP.Shell.Infrastructure.Modules;

namespace HelixERP.Shell.UnitTests;

public sealed class ModuleRegistryTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Registers_finds_and_lists_modules()
    {
        await using var db = TestDb.Create();
        var registry = new ModuleRegistry(db, new TestClock(Now));

        var created = await registry.RegisterAsync(new RegisterModuleRequest("sales", "Sales", "10.0.0.5", 8081));

        created.ModuleKey.Should().Be("sales");
        created.BaseUrl.Should().Be("http://10.0.0.5:8081");
        created.IsActive.Should().BeTrue();
        created.CreatedAtUtc.Should().Be(Now);

        (await registry.ListAsync()).Should().ContainSingle();
        (await registry.FindAsync("SALES")).Should().NotBeNull();
    }

    [Fact]
    public async Task Duplicate_module_keys_are_rejected_case_insensitively()
    {
        await using var db = TestDb.Create();
        var registry = new ModuleRegistry(db, new TestClock(Now));

        await registry.RegisterAsync(new RegisterModuleRequest("sales", "Sales", "10.0.0.5", 8081));

        var action = () => registry.RegisterAsync(new RegisterModuleRequest("Sales", "Sales again", "10.0.0.6", 8082));

        await action.Should().ThrowAsync<DomainException>().WithMessage("*already registered*");
    }

    [Fact]
    public async Task Updates_activation_state_and_removes_modules()
    {
        await using var db = TestDb.Create();
        var registry = new ModuleRegistry(db, new TestClock(Now));
        await registry.RegisterAsync(new RegisterModuleRequest("inventory", "Inventory", "inventory.local", 8090));

        var updated = await registry.UpdateEndpointAsync(
            "inventory",
            new UpdateModuleEndpointRequest("192.168.1.10", 8091));

        updated.Host.Should().Be("192.168.1.10");
        updated.Port.Should().Be(8091);
        updated.UpdatedAtUtc.Should().Be(Now);

        (await registry.SetActiveAsync("inventory", false)).IsActive.Should().BeFalse();
        (await registry.SetActiveAsync("inventory", true)).IsActive.Should().BeTrue();

        (await registry.RemoveAsync("inventory")).Should().BeTrue();
        (await registry.RemoveAsync("inventory")).Should().BeFalse();
        (await registry.ListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_modules_raise_not_found()
    {
        await using var db = TestDb.Create();
        var registry = new ModuleRegistry(db, new TestClock(Now));

        var action = () => registry.SetActiveAsync("missing", false);

        await action.Should().ThrowAsync<KeyNotFoundException>();
        (await registry.FindAsync("missing")).Should().BeNull();
    }

    [Fact]
    public async Task Invalid_endpoints_are_rejected_by_the_aggregate()
    {
        await using var db = TestDb.Create();
        var registry = new ModuleRegistry(db, new TestClock(Now));

        var action = () => registry.RegisterAsync(new RegisterModuleRequest("sales", "Sales", "http://host/path", 8080));

        await action.Should().ThrowAsync<DomainException>();
    }
}