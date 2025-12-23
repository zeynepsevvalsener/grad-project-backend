using FluentValidation;
using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Validators.Nutrition
{
    public class UpdateConsumedFoodRequestDtoValidator : AbstractValidator<UpdateConsumedFoodRequestDto>
    {
        public UpdateConsumedFoodRequestDtoValidator()
        {
            RuleFor(x => x.PortionG)
                .GreaterThan(0)
                .LessThanOrEqualTo(5000);

            RuleFor(x => x.ConsumedAt)
                .Must(d => d == null || d.Value.Year >= 2000)
                .WithMessage("ConsumedAt must be a valid date.");
        }
    }
}
