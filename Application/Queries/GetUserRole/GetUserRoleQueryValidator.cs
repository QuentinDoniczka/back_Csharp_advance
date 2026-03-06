namespace BackBase.Application.Queries.GetUserRole;

using FluentValidation;

public sealed class GetUserRoleQueryValidator : AbstractValidator<GetUserRoleQuery>
{
    public GetUserRoleQueryValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target User ID is required");
    }
}
