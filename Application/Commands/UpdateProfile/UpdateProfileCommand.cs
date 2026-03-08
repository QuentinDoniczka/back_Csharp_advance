namespace BackBase.Application.Commands.UpdateProfile;

using BackBase.Application.DTOs.Output;
using MediatR;

public record UpdateProfileCommand(Guid UserId, string DisplayName, string? AvatarUrl) : IRequest<UserProfileOutput>;
