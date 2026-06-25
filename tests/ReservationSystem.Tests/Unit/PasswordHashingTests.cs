using FluentAssertions;
using Xunit;
using BC = BCrypt.Net.BCrypt;

namespace ReservationSystem.Tests.Unit;

/// <summary>
/// Pins the BCrypt password-hashing behaviour the auth service relies on: hashes
/// differ from the plaintext, the correct password verifies, a wrong one does not.
/// </summary>
public class PasswordHashingTests
{
    private const string Password = "Str0ng!Pass";

    [Fact]
    public void HashPassword_DoesNotReturnPlaintext()
    {
        var hash = BC.HashPassword(Password);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(Password);
        hash.Should().StartWith("$2"); // BCrypt hash prefix
    }

    [Fact]
    public void HashPassword_IsSaltedAndProducesDifferentHashesEachTime()
    {
        BC.HashPassword(Password).Should().NotBe(BC.HashPassword(Password));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = BC.HashPassword(Password);

        BC.Verify(Password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = BC.HashPassword(Password);

        BC.Verify("WrongPass1!", hash).Should().BeFalse();
    }
}
