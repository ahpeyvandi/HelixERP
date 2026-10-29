using FluentAssertions;
using HelixERP.Shell.Infrastructure.Security;

namespace HelixERP.Shell.UnitTests;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 100_000);

    [Fact]
    public void Hash_and_verify_round_trip()
    {
        var hash = _hasher.Hash("Sup3r-Secret!");

        hash.Should().StartWith("pbkdf2-sha256$100000$");
        _hasher.Verify("Sup3r-Secret!", hash).Should().BeTrue();
        _hasher.Verify("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Hashes_use_unique_salts()
    {
        _hasher.Hash("same-password").Should().NotBe(_hasher.Hash("same-password"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("pbkdf2-sha256$abc$xyz$abc")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    public void Verify_returns_false_for_malformed_hashes(string hash)
    {
        _hasher.Verify("password", hash).Should().BeFalse();
    }

    [Fact]
    public void Hasher_requires_strong_iteration_count()
    {
        var action = () => new Pbkdf2PasswordHasher(iterations: 1000);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}