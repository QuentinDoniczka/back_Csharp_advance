namespace BackBase.Application.DTOs.Output;

public record RefreshTokenInfo(Guid UserId, string Jti, DateTime ExpiresAt);
