namespace BackBase.Application.Commands.SetPassword;

using BackBase.Application.Validators;
using FluentValidation;

public sealed class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Password)
            .MustBeStrongPassword();
    }
}
