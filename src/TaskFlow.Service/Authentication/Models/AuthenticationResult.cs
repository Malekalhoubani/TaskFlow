namespace TaskFlow.Service.Authentication.Models;

public class AuthenticationResult
{
    public bool Succeeded { get; set; }

    public string? Message { get; set; }

    public IEnumerable<string> Errors { get; set; } = [];

    public string? AccessToken { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public string? UserId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public IEnumerable<string> Roles { get; set; } = [];
}
