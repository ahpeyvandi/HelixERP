using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.BuildingBlocks.Domain.Platform;
using HelixERP.BuildingBlocks.Domain.Platform.Events;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class ModuleRegistrationTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_normalizes_key_and_host()
    {
        var registration = ModuleRegistration.Create("  Sales  ", " Sales module ", "  LocalHost  ", 5081, Now);

        registration.ModuleKey.Should().Be("sales");
        registration.Host.Should().Be("localhost");
        registration.DisplayName.Should().Be("Sales module");
        registration.Port.Should().Be(5081);
        registration.IsActive.Should().BeTrue();
        registration.CreatedAtUtc.Should().Be(Now);
        registration.BaseUrl.Should().Be("http://localhost:5081");
    }

    [Fact]
    public void Create_raises_module_registered_event()
    {
        var registration = ModuleRegistration.Create("inventory", "Inventory", "10.0.0.5", 5082, Now);

        registration.DomainEvents.Should().ContainSingle();
        var domainEvent = registration.DomainEvents.Single().Should().BeOfType<ModuleRegisteredDomainEvent>().Subject;
        domainEvent.ModuleRegistrationId.Should().Be(registration.Id);
        domainEvent.ModuleKey.Should().Be("inventory");
        domainEvent.OccurredOnUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData("1sales")]
    [InlineData("a")]
    [InlineData("sales_module")]
    [InlineData("sales module")]
    [InlineData("sales.module")]
    public void Create_rejects_invalid_module_keys(string moduleKey)
    {
        var act = () => ModuleRegistration.Create(moduleKey, "Sales", "localhost", 5081, Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_module_keys(string? moduleKey)
    {
        var act = () => ModuleRegistration.Create(moduleKey!, "Sales", "localhost", 5081, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("localhost/path")]
    [InlineData("not a host")]
    public void Create_rejects_malformed_hosts(string host)
    {
        var act = () => ModuleRegistration.Create("sales", "Sales", host, 5081, Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_hosts(string? host)
    {
        var act = () => ModuleRegistration.Create("sales", "Sales", host!, 5081, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Create_rejects_out_of_range_ports(int port)
    {
        var act = () => ModuleRegistration.Create("sales", "Sales", "localhost", port, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_non_utc_timestamp()
    {
        var act = () => ModuleRegistration.Create("sales", "Sales", "localhost", 5081, DateTime.Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_blank_display_name()
    {
        var act = () => ModuleRegistration.Create("sales", "  ", "localhost", 5081, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateEndpoint_changes_address_without_raising_events()
    {
        var registration = ModuleRegistration.Create("sales", "Sales", "localhost", 5081, Now);
        registration.ClearDomainEvents();
        var later = Now.AddMinutes(5);

        registration.UpdateEndpoint("erp-sales", 6001, later);

        registration.Host.Should().Be("erp-sales");
        registration.Port.Should().Be(6001);
        registration.BaseUrl.Should().Be("http://erp-sales:6001");
        registration.UpdatedAtUtc.Should().Be(later);
        registration.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Ipv6_host_is_formatted_with_brackets()
    {
        var registration = ModuleRegistration.Create("sales", "Sales", "::1", 5081, Now);

        registration.BaseUrl.Should().Be("http://[::1]:5081");
    }

    [Fact]
    public void Deactivate_and_activate_are_idempotent()
    {
        var registration = ModuleRegistration.Create("sales", "Sales", "localhost", 5081, Now);
        var firstUpdate = Now.AddMinutes(1);
        var secondUpdate = Now.AddMinutes(2);

        registration.Deactivate(firstUpdate);
        registration.Deactivate(secondUpdate);

        registration.IsActive.Should().BeFalse();
        registration.UpdatedAtUtc.Should().Be(firstUpdate);

        registration.Activate(secondUpdate);
        registration.Activate(secondUpdate.AddMinutes(1));

        registration.IsActive.Should().BeTrue();
        registration.UpdatedAtUtc.Should().Be(secondUpdate);
    }
}