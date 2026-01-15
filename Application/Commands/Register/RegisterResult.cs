namespace BackBase.Application.Commands.Register;

public record RegisterResult(
    Guid Id,
    string Email,
    string FullName
);
