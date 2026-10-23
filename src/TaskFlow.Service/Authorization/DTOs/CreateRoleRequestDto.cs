using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authorization.DTOs;

public class CreateRoleRequestDto
{
    [Required, MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;
}
