using FluentAssertions;
using ReservationSystem.Application.Authentication.Dtos;
using ReservationSystem.Application.Authentication.Validators;
using Xunit;

namespace ReservationSystem.Tests.Unit.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.Validate(new RegisterRequest("alice@example.com", "Str0ng!Pass", "Alice"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("alllower1!")]    // no uppercase
    [InlineData("ALLUPPER1!")]    // no lowercase
    [InlineData("NoDigits!!")]    // no digit
    [InlineData("NoSymbol123")]   // no symbol
    [InlineData("Ab1!")]          // too short
    public void WeakPassword_Fails(string password)
    {
        var result = _validator.Validate(new RegisterRequest("alice@example.com", password, "Alice"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void InvalidEmail_Fails(string email)
    {
        var result = _validator.Validate(new RegisterRequest(email, "Str0ng!Pass", "Alice"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void EmptyFullName_Fails()
    {
        var result = _validator.Validate(new RegisterRequest("alice@example.com", "Str0ng!Pass", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.FullName));
    }
}
