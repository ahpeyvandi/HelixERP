using FluentAssertions;
using HelixERP.Shell.Infrastructure.Security;

namespace HelixERP.Shell.UnitTests;

public sealed class GroupResolverTests
{
    [Fact]
    public void Resolves_parent_groups_transitively()
    {
        var user = Guid.NewGuid();
        var manager = Guid.NewGuid();
        var groupUser = Guid.NewGuid();
        var system = Guid.NewGuid();

        var groups = new Dictionary<Guid, (Guid? ParentId, string Name)>
        {
            [manager] = (groupUser, "sales.group_sales_manager"),
            [groupUser] = (system, "sales.group_sales_user"),
            [system] = (null, "base.group_user"),
            [Guid.NewGuid()] = (null, "stock.group_stock_user")
        };

        var result = GroupResolver.Expand([manager], groups);

        result.GroupNames.Should().BeEquivalentTo(
            ["sales.group_sales_manager", "sales.group_sales_user", "base.group_user"]);
        result.GroupIds.Should().HaveCount(3);
    }

    [Fact]
    public void Tolerates_cycles_and_unknown_groups()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var unknown = Guid.NewGuid();

        var groups = new Dictionary<Guid, (Guid? ParentId, string Name)>
        {
            [first] = (second, "a.group_one"),
            [second] = (first, "a.group_two")
        };

        var result = GroupResolver.Expand([first, unknown], groups);

        result.GroupNames.Should().BeEquivalentTo(["a.group_one", "a.group_two"]);
    }
}