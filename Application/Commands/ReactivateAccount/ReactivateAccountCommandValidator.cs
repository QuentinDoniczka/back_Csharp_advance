namespace BackBase.Application.Commands.ReactivateAccount;

using FluentValidation;

public sealed class ReactivateAccountCommandValidator : AbstractValidator<ReactivateAccountCommand>
{
    public ReactivateAccountCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target User ID is required");
    }
}
