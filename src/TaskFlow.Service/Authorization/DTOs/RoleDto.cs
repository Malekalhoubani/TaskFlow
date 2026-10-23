namespace TaskFlow.Service.Authorization.DTOs;

public class RoleDto
{
    public string Name { get; set; } = string.Empty;
    public IEnumerable<string> Permissions { get; set; } = [];
}
