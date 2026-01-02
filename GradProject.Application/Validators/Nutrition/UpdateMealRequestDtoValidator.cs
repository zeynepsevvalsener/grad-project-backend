using FluentValidation;
using GradProject.Application.DTOs.Nutrition;
using GradProject.Domain.Enums;

namespace GradProject.Application.Validators.Nutrition
{
    public class UpdateMealRequestDtoValidator : AbstractValidator<UpdateMealRequestDto>
    {
        public UpdateMealRequestDtoValidator()
        {
            RuleFor(x => x.MealType)
                .IsInEnum()
                .WithMessage("MealType must be a valid enum value.");

            RuleFor(x => x.LoggedAt)
                .NotEmpty()
                .Must(d => d.Year >= 2000)
                .WithMessage("LoggedAt must be a valid date with year >= 2000.");

            RuleFor(x => x.RawText)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.RawText))
                .WithMessage("RawText must not exceed 1000 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Notes))
                .WithMessage("Notes must not exceed 500 characters.");

            RuleForEach(x => x.Foods)
                .SetValidator(new MealFoodDtoValidator())
                .When(x => x.Foods != null && x.Foods.Any());

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.RawText) || (x.Foods != null && x.Foods.Any()))
                .WithMessage("At least one of RawText or Foods must be provided.");
        }
    }
}

