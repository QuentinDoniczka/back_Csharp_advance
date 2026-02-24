namespace BackBase.Application.Commands.RefreshToken;

using MediatR;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<RefreshTokenResult>;
