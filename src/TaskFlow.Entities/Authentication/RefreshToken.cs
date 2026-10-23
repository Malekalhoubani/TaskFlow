using TaskFlow.Entities.Users;

namespace TaskFlow.Entities.Authentication;

public class RefreshToken
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    // Groups all rotated refresh tokens from the same login session.
    // If an old token is reused, the entire token family can be revoked.
    public Guid FamilyId { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? CreatedByIp { get; set; }

    public string? RevokedByIp { get; set; }

    public string? UserAgent { get; set; }

    public string? RevocationReason { get; set; }

    public ApplicationUser User { get; set; } = null!;
}