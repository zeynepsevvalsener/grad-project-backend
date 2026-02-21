using FluentValidation;
using GradProject.Application.DTOs.Auth;

namespace GradProject.Application.Validators.Auth
{
    public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("validation.email.required")
                .EmailAddress().WithMessage("validation.email.invalid")
                .MaximumLength(256).WithMessage("validation.email.maxLength");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("validation.password.required")
                .MinimumLength(8).WithMessage("validation.password.minLength")
                .MaximumLength(100).WithMessage("validation.password.maxLength");
        }
    }
}
