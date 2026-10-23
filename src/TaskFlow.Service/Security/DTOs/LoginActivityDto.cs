namespace TaskFlow.Service.Security.DTOs;

public class LoginActivityDto
{
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
