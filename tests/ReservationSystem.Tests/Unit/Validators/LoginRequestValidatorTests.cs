using FluentAssertions;
using ReservationSystem.Application.Authentication.Dtos;
using ReservationSystem.Application.Authentication.Validators;
using Xunit;

namespace ReservationSystem.Tests.Unit.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new LoginRequest("alice@example.com", "secret")).IsValid.Should().BeTrue();

    [Fact]
    public void InvalidEmail_Fails() =>
        _validator.Validate(new LoginRequest("nope", "secret")).IsValid.Should().BeFalse();

    [Fact]
    public void EmptyPassword_Fails() =>
        _validator.Validate(new LoginRequest("alice@example.com", "")).IsValid.Should().BeFalse();
}
