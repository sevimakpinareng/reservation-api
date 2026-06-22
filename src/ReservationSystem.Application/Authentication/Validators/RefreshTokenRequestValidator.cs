using FluentValidation;
using ReservationSystem.Application.Authentication.Dtos;

namespace ReservationSystem.Application.Authentication.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
