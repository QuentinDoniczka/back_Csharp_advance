namespace BackBase.Application.Queries.GetUserProfile;

using BackBase.Application.DTOs.Output;
using MediatR;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileOutput>;
