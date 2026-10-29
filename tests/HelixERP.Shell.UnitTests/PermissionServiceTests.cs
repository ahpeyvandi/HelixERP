using FluentAssertions;
using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.Shell.Infrastructure.Security;

namespace HelixERP.Shell.UnitTests;

public sealed class PermissionServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Anonymous_user_has_no_access()
    {
        await using var db = TestDb.Create();
        var service = new PermissionService(db, TestCurrentUser.Anonymous());

        (await service.HasAccessAsync("sales.order", OperationType.Read)).Should().BeFalse();
        (await service.GetRecordRulesAsync("sales.order", OperationType.Read)).Should().BeEmpty();
    }

    [Fact]
    public async Task Superuser_bypasses_access_rights()
    {
        await using var db = TestDb.Create();
        var user = User.Create("root", "root@example.com", "Root", "hash", isSuperUser: true, createdAtUtc: Now);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new PermissionService(db, TestCurrentUser.For(user.Id, user.Login));

        (await service.HasAccessAsync("sales.order", OperationType.Unlink)).Should().BeTrue();
    }

    [Fact]
    public async Task Group_inheritance_grants_parent_access_rights()
    {
        await using var db = TestDb.Create();

        var parent = Group.Create("sales.group_sales_user", "User", "Sales", createdAtUtc: Now);
        var child = Group.Create("sales.group_sales_manager", "Manager", "Sales", parent.Id, createdAtUtc: Now);
        var user = User.Create("seller", "seller@example.com", "Seller", "hash", createdAtUtc: Now);
        user.AssignToGroup(child.Id, Now);

        db.Groups.AddRange(parent, child);
        db.Users.Add(user);
        db.AccessRights.Add(AccessRight.Create(parent.Id, "sales.order", AccessRightFlags.Read, createdAtUtc: Now));
        await db.SaveChangesAsync();

        var service = new PermissionService(db, TestCurrentUser.For(user.Id, user.Login));

        (await service.HasAccessAsync("sales.order", OperationType.Read)).Should().BeTrue();
        (await service.HasAccessAsync("sales.order", OperationType.Write)).Should().BeFalse();
        (await service.GetGroupsAsync(user.Id)).Should().BeEquivalentTo(
            ["sales.group_sales_manager", "sales.group_sales_user"]);
    }

    [Fact]
    public async Task Inactive_users_are_denied()
    {
        await using var db = TestDb.Create();

        var group = Group.Create("base.group_user", "Internal User", "Administration", createdAtUtc: Now);
        var user = User.Create("inactive", "inactive@example.com", "Inactive", "hash", createdAtUtc: Now);
        user.AssignToGroup(group.Id, Now);
        user.Deactivate();

        db.Groups.Add(group);
        db.Users.Add(user);
        db.AccessRights.Add(AccessRight.Create(group.Id, "base.attachment", AccessRightFlags.All, createdAtUtc: Now));
        await db.SaveChangesAsync();

        var service = new PermissionService(db, TestCurrentUser.For(user.Id, user.Login));

        (await service.HasAccessAsync("base.attachment", OperationType.Read)).Should().BeFalse();
    }

    [Fact]
    public async Task Record_rules_are_filtered_by_group_and_operation()
    {
        await using var db = TestDb.Create();

        var group = Group.Create("sales.group_sales_user", "User", "Sales", createdAtUtc: Now);
        var otherGroup = Group.Create("stock.group_stock_user", "User", "Inventory", createdAtUtc: Now);
        var user = User.Create("seller", "seller@example.com", "Seller", "hash", createdAtUtc: Now);
        user.AssignToGroup(group.Id, Now);

        db.Groups.AddRange(group, otherGroup);
        db.Users.Add(user);
        db.RecordRules.AddRange(
            RecordRule.Create("Global read", "sales.order", "[]", null, AccessRightFlags.Read, createdAtUtc: Now),
            RecordRule.Create("Own records", "sales.order", "[[\"user_id\", \"=\", 1]]", group.Id, AccessRightFlags.Read | AccessRightFlags.Write, createdAtUtc: Now),
            RecordRule.Create("Stock only", "sales.order", "[[\"state\", \"=\", \"draft\"]]", otherGroup.Id, AccessRightFlags.Read, createdAtUtc: Now),
            RecordRule.Create("Sales create", "sales.order", "[]", group.Id, AccessRightFlags.Create, createdAtUtc: Now));
        await db.SaveChangesAsync();

        var service = new PermissionService(db, TestCurrentUser.For(user.Id, user.Login));

        var readRules = await service.GetRecordRulesAsync("sales.order", OperationType.Read);
        readRules.Select(rule => rule.Name).Should().BeEquivalentTo(["Global read", "Own records"]);
        readRules.Single(rule => rule.Name == "Global read").IsGlobal.Should().BeTrue();

        var createRules = await service.GetRecordRulesAsync("sales.order", OperationType.Create);
        createRules.Select(rule => rule.Name).Should().ContainSingle().Which.Should().Be("Sales create");
    }
}