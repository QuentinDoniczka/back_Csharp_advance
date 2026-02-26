namespace BackBase.Application.Commands.GoogleLogin;

using BackBase.Application.DTOs.Output;
using MediatR;

public record GoogleLoginCommand(string IdToken) : IRequest<GoogleLoginResult>;
