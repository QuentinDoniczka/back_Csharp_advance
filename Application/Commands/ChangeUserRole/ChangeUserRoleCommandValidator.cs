namespace BackBase.Application.Commands.ChangeUserRole;

using FluentValidation;

public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.CallerUserId)
            .NotEmpty().WithMessage("Caller User ID is required");

        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target User ID is required");

        RuleFor(x => x.NewRole)
            .NotEmpty().WithMessage("Role is required");
    }
}
