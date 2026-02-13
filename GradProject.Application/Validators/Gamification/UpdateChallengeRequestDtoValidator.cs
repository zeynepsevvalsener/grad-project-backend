using FluentValidation;
using GradProject.Application.DTOs.Gamification;
using GradProject.Domain.Enums;

namespace GradProject.Application.Validators.Gamification
{
    public class UpdateChallengeRequestDtoValidator : AbstractValidator<UpdateChallengeRequestDto>
    {
        public UpdateChallengeRequestDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Type must be a valid ChallengeType enum value.");

            RuleFor(x => x.Metric)
                .IsInEnum()
                .WithMessage("Metric must be a valid ChallengeMetric enum value.");

            RuleFor(x => x.TargetValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("TargetValue must be greater than or equal to 0.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("StartDate is required.")
                .Must(d => d.Year >= 2000)
                .WithMessage("StartDate must be a valid date with year >= 2000.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("EndDate is required.")
                .Must(d => d.Year >= 2000)
                .WithMessage("EndDate must be a valid date with year >= 2000.")
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("EndDate must be greater than or equal to StartDate.");

            RuleFor(x => x.RewardPoints)
                .GreaterThanOrEqualTo(0)
                .WithMessage("RewardPoints must be greater than or equal to 0.");
        }
    }
}

