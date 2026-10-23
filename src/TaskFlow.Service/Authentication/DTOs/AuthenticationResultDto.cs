using Azure;
using System.Text.Json.Serialization;

namespace TaskFlow.Service.Authentication.DTOs;

public class AuthenticationResultDto
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

    public bool RequiresTwoFactor { get; set; }

    // prevents the refresh token from appearing in the JSON response
    [JsonIgnore]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
