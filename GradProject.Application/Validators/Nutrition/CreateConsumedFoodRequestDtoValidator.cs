using FluentValidation;
using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Validators.Nutrition
{
    public class CreateConsumedFoodRequestDtoValidator : AbstractValidator<CreateConsumedFoodRequestDto>
    {
        public CreateConsumedFoodRequestDtoValidator()
        {
            RuleFor(x => x.FoodId)
                .GreaterThan(0);

            RuleFor(x => x.PortionG)
                .GreaterThan(0)
                .LessThanOrEqualTo(5000); // sanity limit (5kg)

            RuleFor(x => x.ConsumedAt)
                .Must(d => d == null || d.Value.Year >= 2000)
                .WithMessage("ConsumedAt must be a valid date.");
        }
    }
}
