using FluentAssertions;
using ReservationSystem.Application.Services.Dtos;
using ReservationSystem.Application.Services.Validators;
using Xunit;

namespace ReservationSystem.Tests.Unit.Validators;

public class CreateServiceRequestValidatorTests
{
    private readonly CreateServiceRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.Validate(new CreateServiceRequest("Haircut", "Basic", 30, 25m));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyName_Fails()
    {
        var result = _validator.Validate(new CreateServiceRequest("", null, 30, 25m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceRequest.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void NonPositiveDuration_Fails(int duration)
    {
        var result = _validator.Validate(new CreateServiceRequest("Haircut", null, duration, 25m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceRequest.DurationMinutes));
    }

    [Fact]
    public void NegativePrice_Fails()
    {
        var result = _validator.Validate(new CreateServiceRequest("Haircut", null, 30, -1m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceRequest.Price));
    }
}
