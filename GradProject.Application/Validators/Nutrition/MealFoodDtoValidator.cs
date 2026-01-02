using FluentValidation;
using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Validators.Nutrition
{
    public class MealFoodDtoValidator : AbstractValidator<MealFoodDto>
    {
        public MealFoodDtoValidator()
        {
            RuleFor(x => x.FoodId)
                .GreaterThan(0)
                .WithMessage("FoodId must be greater than 0.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0.1m)
                .LessThanOrEqualTo(10000)
                .WithMessage("Quantity must be between 0.1 and 10000.");

            RuleFor(x => x.Unit)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Unit must be a non-empty string with maximum 50 characters.");
        }
    }
}

