using TaskFlow.Entities.Users;

namespace TaskFlow.Entities.Security;

public class LoginActivity
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ApplicationUser? User { get; set; }
}
