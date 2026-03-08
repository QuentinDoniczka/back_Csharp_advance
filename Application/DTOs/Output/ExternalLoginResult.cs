namespace BackBase.Application.DTOs.Output;

public record ExternalLoginResult(Guid UserId, string Email, bool IsNewAccount);
