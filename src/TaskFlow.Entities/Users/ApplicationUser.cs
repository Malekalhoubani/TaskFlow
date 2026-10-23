using Microsoft.AspNetCore.Identity;
using TaskFlow.Entities.Authentication;
using TaskFlow.Entities.Security;

namespace TaskFlow.Entities.Users;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<LoginActivity> LoginActivities { get; set; } = [];
}
