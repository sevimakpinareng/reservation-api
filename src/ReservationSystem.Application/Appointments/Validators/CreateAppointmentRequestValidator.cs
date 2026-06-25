using FluentValidation;
using ReservationSystem.Application.Appointments.Dtos;

namespace ReservationSystem.Application.Appointments.Validators;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("ServiceId is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required.")
            .Must(start => start.ToUniversalTime() > DateTime.UtcNow)
            .WithMessage("StartTime must be in the future.");
    }
}
