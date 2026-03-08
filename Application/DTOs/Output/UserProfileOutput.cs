using BackBase.Domain.Entities;

namespace BackBase.Application.DTOs.Output;

public record UserProfileOutput(Guid Id, Guid UserId, string DisplayName, string? AvatarUrl, DateTime CreatedAt, DateTime UpdatedAt, bool IsDeactivated)
{
    public static UserProfileOutput FromEntity(UserProfile profile)
    {
        return new UserProfileOutput(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.AvatarUrl,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.IsDeactivated);
    }
}
