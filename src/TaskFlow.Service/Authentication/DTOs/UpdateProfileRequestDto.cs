using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authentication.DTOs;

public class UpdateProfileRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}