namespace BackBase.Application.DTOs.Output;

public record GoogleLoginResult(AuthTokenResult Tokens, bool IsNewAccount);
