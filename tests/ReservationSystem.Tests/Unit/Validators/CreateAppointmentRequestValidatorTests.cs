using FluentAssertions;
using ReservationSystem.Application.Appointments.Dtos;
using ReservationSystem.Application.Appointments.Validators;
using Xunit;

namespace ReservationSystem.Tests.Unit.Validators;

public class CreateAppointmentRequestValidatorTests
{
    private readonly CreateAppointmentRequestValidator _validator = new();

    [Fact]
    public void FutureStart_Passes()
    {
        var result = _validator.Validate(new CreateAppointmentRequest(Guid.NewGuid(), DateTime.UtcNow.AddDays(1)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PastStart_Fails()
    {
        var result = _validator.Validate(new CreateAppointmentRequest(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppointmentRequest.StartTime));
    }

    [Fact]
    public void EmptyServiceId_Fails()
    {
        var result = _validator.Validate(new CreateAppointmentRequest(Guid.Empty, DateTime.UtcNow.AddDays(1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppointmentRequest.ServiceId));
    }
}
