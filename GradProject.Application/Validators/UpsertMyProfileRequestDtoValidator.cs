using FluentValidation;
using GradProject.Application.DTOs.Profile;
using GradProject.Domain.Enums;

namespace GradProject.Application.Validators.Profile
{
    public class UpsertMyProfileRequestDtoValidator
        : AbstractValidator<UpsertMyProfileRequestDto>
    {
        public UpsertMyProfileRequestDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .MaximumLength(100);

            RuleFor(x => x.DateOfBirth)
                .NotNull()
                .Must(dob =>
                {
                    if (dob is null) return false;

                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var d = DateOnly.FromDateTime(dob.Value);

                    var age = today.Year - d.Year;
                    if (d > today.AddYears(-age)) age--;

                    return age >= 10 && age <= 120;
                })
                .WithMessage("Age must be between 10 and 120 to calculate TDEE.");


            RuleFor(x => x.Gender)
                .NotEqual(Gender.Unknown)
                .WithMessage("Please select your gender.");

            RuleFor(x => x.Height)
                .NotNull()
                .GreaterThan(50)   // cm
                .LessThan(300);

            RuleFor(x => x.Weight)
                .NotNull()
                .GreaterThan(20)   // kg
                .LessThan(500);

            RuleFor(x => x.ActivityLevel)
                .IsInEnum();
        }
    }
}
