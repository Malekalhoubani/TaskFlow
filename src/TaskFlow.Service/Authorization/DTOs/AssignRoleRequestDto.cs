using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authorization.DTOs;

public class AssignRoleRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RoleName { get; set; } = string.Empty;
}
