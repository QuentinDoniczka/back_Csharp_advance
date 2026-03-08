namespace BackBase.Infrastructure.Authentication;

public sealed class GoogleAuthSettings
{
    public const string SectionName = "GoogleAuth";

    public string ClientId { get; init; } = string.Empty;
}
