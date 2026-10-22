using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Entities.Users;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
