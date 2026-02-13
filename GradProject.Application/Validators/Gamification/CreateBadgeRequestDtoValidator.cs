using FluentValidation;
using GradProject.Application.DTOs.Gamification;
using GradProject.Domain.Enums;

namespace GradProject.Application.Validators.Gamification
{
    public class CreateBadgeRequestDtoValidator : AbstractValidator<CreateBadgeRequestDto>
    {
        public CreateBadgeRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Type must be a valid BadgeType enum value.");

            RuleFor(x => x.IconUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.IconUrl))
                .WithMessage("IconUrl must not exceed 500 characters.")
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.IconUrl))
                .WithMessage("IconUrl must be a valid URL.");

            RuleFor(x => x.PointsReward)
                .GreaterThanOrEqualTo(0)
                .WithMessage("PointsReward must be greater than or equal to 0.");
        }
    }
}

