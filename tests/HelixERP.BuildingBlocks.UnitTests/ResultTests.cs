using FluentAssertions;
using HelixERP.BuildingBlocks.Shared.Results;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_result_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_result_carries_the_error()
    {
        var error = Error.Validation("validation.failed", "Invalid input");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Generic_success_exposes_value()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Implicit_conversion_creates_success_result()
    {
        Result<string> result = "helix";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("helix");
    }

    [Fact]
    public void Accessing_value_of_failure_throws()
    {
        var result = Result.Failure<int>(Error.NotFound("not.found", "Missing"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Success_with_error_is_invalid()
    {
        var act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_factories_set_the_error_type()
    {
        Error.Validation("a", "a").Type.Should().Be(ErrorType.Validation);
        Error.NotFound("a", "a").Type.Should().Be(ErrorType.NotFound);
        Error.Conflict("a", "a").Type.Should().Be(ErrorType.Conflict);
        Error.Unauthorized("a", "a").Type.Should().Be(ErrorType.Unauthorized);
        Error.Forbidden("a", "a").Type.Should().Be(ErrorType.Forbidden);
    }
}