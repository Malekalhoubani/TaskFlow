using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authentication.DTOs;

public class ChangeEmailRequestDto
{
    [Required]
    [EmailAddress]
    public string NewEmail { get; set; } = string.Empty;

    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
}