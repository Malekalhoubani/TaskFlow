namespace TaskFlow.Service.Authorization.DTOs;

public class UserSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
}
