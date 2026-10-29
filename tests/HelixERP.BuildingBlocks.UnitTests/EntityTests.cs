using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class EntityTests
{
    [Fact]
    public void Entities_with_same_type_and_id_are_equal()
    {
        var id = Guid.NewGuid();

        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Entities_of_different_types_are_not_equal()
    {
        var id = Guid.NewGuid();

        var first = new TestEntity(id);
        Entity<Guid> second = new OtherTestEntity(id);

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Transient_entities_are_never_equal()
    {
        var first = new TransientTestEntity();
        var second = new TransientTestEntity();

        first.Equals(second).Should().BeFalse();
        first.Equals(first).Should().BeTrue();
    }

    [Fact]
    public void Entity_is_not_equal_to_null()
    {
        Entity<Guid>? other = null;
        var entity = new TestEntity(Guid.NewGuid());

        entity.Equals(other).Should().BeFalse();
        (entity == other).Should().BeFalse();
    }
}