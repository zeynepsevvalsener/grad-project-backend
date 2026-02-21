using FluentValidation;
using GradProject.Application.DTOs.Auth;

namespace GradProject.Application.Validators.Auth
{
    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("validation.email.required")
                .EmailAddress().WithMessage("validation.email.invalid")
                .MaximumLength(256).WithMessage("validation.email.maxLength");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("validation.password.required");
        }
    }
}
