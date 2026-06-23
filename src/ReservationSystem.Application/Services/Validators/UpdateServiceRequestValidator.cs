using FluentValidation;
using ReservationSystem.Application.Services.Dtos;

namespace ReservationSystem.Application.Services.Validators;

public class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(ServiceValidationRules.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(ServiceValidationRules.DescriptionMaxLength);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.")
            .LessThanOrEqualTo(ServiceValidationRules.MaxDurationMinutes)
            .WithMessage($"Duration must not exceed {ServiceValidationRules.MaxDurationMinutes} minutes.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
    }
}
