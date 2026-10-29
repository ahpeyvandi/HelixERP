using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.BuildingBlocks.Domain.Platform;
using HelixERP.BuildingBlocks.Domain.Security;

namespace HelixERP.Shell.UnitTests;

public sealed class SecurityDomainTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void User_normalizes_login_and_email()
    {
        var user = User.Create("  John.Doe ", "John.Doe@Example.COM", "John Doe", "hash", createdAtUtc: Now);

        user.Login.Should().Be("john.doe");
        user.Email.Should().Be("john.doe@example.com");
        user.IsActive.Should().BeTrue();
        user.IsSuperUser.Should().BeFalse();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("UPPER!")]
    public void User_rejects_invalid_logins(string login)
    {
        var action = () => User.Create(login, "user@example.com", "User", "hash", createdAtUtc: Now);

        action.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@@example.com")]
    public void User_rejects_invalid_email(string email)
    {
        var action = () => User.Create("valid.user", email, "User", "hash", createdAtUtc: Now);

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void User_group_assignment_is_idempotent_and_removable()
    {
        var user = User.Create("john.doe", "john@example.com", "John", "hash", createdAtUtc: Now);
        var groupId = Guid.NewGuid();

        user.AssignToGroup(groupId, Now);
        user.AssignToGroup(groupId, Now);

        user.Memberships.Should().HaveCount(1);
        user.Memberships.Single().GroupId.Should().Be(groupId);

        user.RemoveFromGroup(groupId).Should().BeTrue();
        user.RemoveFromGroup(groupId).Should().BeFalse();
        user.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void User_lifecycle_updates_state()
    {
        var user = User.Create("john.doe", "john@example.com", "John", "hash", createdAtUtc: Now);

        user.Deactivate();
        user.IsActive.Should().BeFalse();

        user.Activate();
        user.IsActive.Should().BeTrue();

        user.RecordLogin(Now.AddMinutes(1));
        user.LastLoginAtUtc.Should().Be(Now.AddMinutes(1));

        user.GrantSuperUser();
        user.IsSuperUser.Should().BeTrue();

        user.SetPasswordHash("new-hash");
        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public void Group_requires_full_external_id_and_parent_cannot_be_itself()
    {
        var group = Group.Create("base.group_system", "Settings", "Administration", createdAtUtc: Now);

        group.Name.Should().Be("base.group_system");
        group.ParentId.Should().BeNull();

        var action = () => group.SetParent(group.Id);
        action.Should().Throw<DomainException>();

        var invalid = () => Group.Create("invalid", "Invalid", "Administration", createdAtUtc: Now);
        invalid.Should().Throw<DomainException>();
    }

    [Fact]
    public void Access_right_maps_flags_correctly()
    {
        var right = AccessRight.Create(
            Guid.NewGuid(),
            "Sales.Order",
            AccessRightFlags.Read | AccessRightFlags.Create,
            createdAtUtc: Now);

        right.Model.Should().Be("sales.order");
        right.Allows(AccessRightFlags.Read).Should().BeTrue();
        right.Allows(AccessRightFlags.Create).Should().BeTrue();
        right.Allows(AccessRightFlags.Write).Should().BeFalse();
        right.Allows(AccessRightFlags.None).Should().BeFalse();

        var action = () => AccessRight.Create(Guid.NewGuid(), "sales.order", AccessRightFlags.None, createdAtUtc: Now);
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Record_rule_validates_json_domain_and_lifecycle()
    {
        var rule = RecordRule.Create(
            "Own orders",
            "sales.order",
            "[[\"user_id\", \"=\", 1]]",
            Guid.NewGuid(),
            AccessRightFlags.Read | AccessRightFlags.Write,
            createdAtUtc: Now);

        rule.IsGlobal.Should().BeFalse();
        rule.IsActive.Should().BeTrue();

        rule.SetActive(false, Now.AddMinutes(1));
        rule.IsActive.Should().BeFalse();
        rule.UpdatedAtUtc.Should().Be(Now.AddMinutes(1));

        var invalidJson = () => RecordRule.Create("Broken", "sales.order", "{", null, AccessRightFlags.Read, createdAtUtc: Now);
        invalidJson.Should().Throw<DomainException>();

        var notAnArray = () => RecordRule.Create("Broken", "sales.order", "{\"a\":1}", null, AccessRightFlags.Read, createdAtUtc: Now);
        notAnArray.Should().Throw<DomainException>();
    }

    [Fact]
    public void Refresh_token_can_only_be_revoked_once()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(1), Now);

        token.IsActiveAt(Now).Should().BeTrue();
        token.Revoke(Now.AddMinutes(5), "replacement");

        token.IsActiveAt(Now.AddMinutes(6)).Should().BeFalse();
        token.ReplacedByTokenHash.Should().Be("replacement");

        var action = () => token.Revoke(Now.AddMinutes(7));
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void File_attachment_rejects_path_traversal()
    {
        var action = () => FileAttachment.Create("../../etc/passwd", "2026/10/file.bin", "application/octet-stream", 10, createdAtUtc: Now);
        action.Should().Throw<DomainException>();

        var unsafeKey = () => FileAttachment.Create("report.pdf", "../escape/report.pdf", "application/pdf", 10, createdAtUtc: Now);
        unsafeKey.Should().Throw<DomainException>();

        var file = FileAttachment.Create("report.pdf", "2026/10/abc.pdf", "application/pdf", 10, createdAtUtc: Now);
        file.MarkDeleted(Now.AddMinutes(1));

        file.IsDeleted.Should().BeTrue();
        file.DeletedAtUtc.Should().Be(Now.AddMinutes(1));
    }
}