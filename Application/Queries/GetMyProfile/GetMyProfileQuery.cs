namespace BackBase.Application.Queries.GetMyProfile;

using BackBase.Application.DTOs.Output;
using MediatR;

public record GetMyProfileQuery(Guid UserId) : IRequest<UserProfileOutput>;
