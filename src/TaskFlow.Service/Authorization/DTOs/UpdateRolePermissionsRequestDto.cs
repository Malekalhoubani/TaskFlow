using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authorization.DTOs;

public class UpdateRolePermissionsRequestDto
{
    [Required]
    public string RoleName { get; set; } = string.Empty;

    public IEnumerable<string> Permissions { get; set; } = [];
}
