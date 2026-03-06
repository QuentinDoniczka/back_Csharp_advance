namespace BackBase.Application.Commands.UpdateProfile;

using FluentValidation;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Display name must not exceed 50 characters");

        RuleFor(x => x.AvatarUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl))
            .WithMessage("Avatar URL must be a valid URI");
    }
}
