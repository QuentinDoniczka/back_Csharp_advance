namespace BackBase.Application.Commands.RefreshToken;

using BackBase.Application.DTOs.Output;
using MediatR;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokenResult>;
