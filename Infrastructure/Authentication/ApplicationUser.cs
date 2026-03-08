namespace BackBase.Infrastructure.Authentication;

using Microsoft.AspNetCore.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTime? BannedUntil { get; private set; }
}
