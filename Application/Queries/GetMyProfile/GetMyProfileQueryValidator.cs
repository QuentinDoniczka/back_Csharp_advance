namespace BackBase.Application.Queries.GetMyProfile;

using FluentValidation;

public sealed class GetMyProfileQueryValidator : AbstractValidator<GetMyProfileQuery>
{
    public GetMyProfileQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}
