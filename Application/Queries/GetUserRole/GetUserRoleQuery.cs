namespace BackBase.Application.Queries.GetUserRole;

using BackBase.Application.DTOs.Output;
using MediatR;

public record GetUserRoleQuery(Guid TargetUserId) : IRequest<UserRoleOutput>;
