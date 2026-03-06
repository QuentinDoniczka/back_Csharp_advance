using BackBase.Application.DTOs.Output;

namespace BackBase.API.DTOs;

public record UserProfileResponseDto(Guid Id, Guid UserId, string DisplayName, string? AvatarUrl, DateTime CreatedAt, DateTime UpdatedAt, bool IsDeactivated)
{
    public static UserProfileResponseDto FromOutput(UserProfileOutput output)
    {
        return new UserProfileResponseDto(
            output.Id,
            output.UserId,
            output.DisplayName,
            output.AvatarUrl,
            output.CreatedAt,
            output.UpdatedAt,
            output.IsDeactivated);
    }
}
