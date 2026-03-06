namespace BackBase.Domain.Entities;

public sealed class UserProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    public bool IsDeactivated => DeactivatedAt.HasValue;

    private UserProfile() { }

    public static UserProfile Create(Guid userId, string displayName, string? avatarUrl = null)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string displayName, string? avatarUrl)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (IsDeactivated)
            throw new InvalidOperationException("Account is already deactivated.");

        DeactivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (!IsDeactivated)
            throw new InvalidOperationException("Account is not deactivated.");

        DeactivatedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
