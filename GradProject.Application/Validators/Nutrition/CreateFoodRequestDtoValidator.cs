using FluentValidation;
using GradProject.Application.DTOs.Nutrition;

namespace GradProject.Application.Validators.Nutrition
{
    public class CreateFoodRequestDtoValidator : AbstractValidator<CreateFoodRequestDto>
    {
        public CreateFoodRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Category)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Source)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Kcal).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ProteinG).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FatG).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CarbG).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SugarG).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FiberG).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SodiumMg).GreaterThanOrEqualTo(0);

            RuleFor(x => x.DefaultPortionG)
                .GreaterThan(0);

           
        }
    }
}
