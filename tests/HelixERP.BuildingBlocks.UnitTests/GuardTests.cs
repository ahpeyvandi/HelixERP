using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Common;
using HelixERP.BuildingBlocks.Domain.Exceptions;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class GuardTests
{
    [Fact]
    public void NotNull_returns_value_when_not_null()
    {
        var value = new object();

        Guard.NotNull(value, "value").Should().BeSameAs(value);
    }

    [Fact]
    public void NotNull_throws_when_null()
    {
        var act = () => Guard.NotNull<object>(null, "value");

        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_throws_for_empty_values(string? value)
    {
        var act = () => Guard.NotNullOrWhiteSpace(value, "text");

        act.Should().Throw<ArgumentException>().WithParameterName("text");
    }

    [Fact]
    public void NotNullOrWhiteSpace_returns_original_value()
    {
        Guard.NotNullOrWhiteSpace("  hello  ", "text").Should().Be("  hello  ");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InRange_throws_when_value_is_outside_range(int value)
    {
        var act = () => Guard.InRange(value, 1, 100, "value");

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
    }

    [Fact]
    public void InRange_returns_value_when_inside_range()
    {
        Guard.InRange(50, 1, 100, "value").Should().Be(50);
    }

    [Fact]
    public void Against_throws_domain_exception_when_condition_is_true()
    {
        var act = () => Guard.Against(true, "rule broken");

        act.Should().Throw<DomainException>().WithMessage("rule broken");
    }

    [Fact]
    public void Against_does_nothing_when_condition_is_false()
    {
        var act = () => Guard.Against(false, "rule broken");

        act.Should().NotThrow();
    }
}