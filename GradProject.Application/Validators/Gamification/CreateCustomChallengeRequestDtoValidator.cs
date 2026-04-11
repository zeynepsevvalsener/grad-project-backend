using FluentValidation;
using GradProject.Application.Configuration;
using GradProject.Application.DTOs.Gamification;
using GradProject.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GradProject.Application.Validators.Gamification
{
    public class CreateCustomChallengeRequestDtoValidator : AbstractValidator<CreateCustomChallengeRequestDto>
    {
        public CreateCustomChallengeRequestDtoValidator(IOptionsSnapshot<CustomChallengeSettings> options)
        {
            var s = options.Value;

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x.GoalType)
                .IsInEnum()
                .WithMessage("GoalType must be Distance or Calories.");

            RuleFor(x => x.TargetValue)
                .GreaterThan(0)
                .WithMessage("TargetValue must be greater than 0.");

            RuleFor(x => x.TargetValue)
                .LessThanOrEqualTo(s.MaxTargetDistanceMeters)
                .When(x => x.GoalType == CustomChallengeGoalType.Distance)
                .WithMessage($"Target distance must not exceed {s.MaxTargetDistanceMeters} meters.");

            RuleFor(x => x.TargetValue)
                .LessThanOrEqualTo(s.MaxTargetCalories)
                .When(x => x.GoalType == CustomChallengeGoalType.Calories)
                .WithMessage($"Target calories must not exceed {s.MaxTargetCalories}.");

            RuleFor(x => x.StartDate)
                .Must(d => !d.HasValue || d.Value.Year >= 2000)
                .WithMessage("StartDate year must be >= 2000 when provided.");

            RuleFor(x => x.EndDate)
                .Must(d => !d.HasValue || d.Value.Year >= 2000)
                .WithMessage("EndDate year must be >= 2000 when provided.");

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate > x.StartDate)
                .WithMessage("EndDate must be after StartDate when both are provided.");
        }
    }
}
