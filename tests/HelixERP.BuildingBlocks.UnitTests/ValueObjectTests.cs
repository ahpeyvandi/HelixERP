using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }

        public string Currency { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class OtherMoney : ValueObject
    {
        public OtherMoney(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }

        public string Currency { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Value_objects_with_equal_components_are_equal()
    {
        var first = new Money(100m, "IRR");
        var second = new Money(100m, "IRR");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Value_objects_with_different_components_are_not_equal()
    {
        var first = new Money(100m, "IRR");
        var second = new Money(100m, "USD");

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Value_objects_of_different_types_are_not_equal_even_with_same_components()
    {
        var first = new Money(100m, "IRR");
        var second = new OtherMoney(100m, "IRR");

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Value_object_is_not_equal_to_null()
    {
        var money = new Money(10m, "IRR");

        money.Equals(null).Should().BeFalse();
        (money == null).Should().BeFalse();
    }
}