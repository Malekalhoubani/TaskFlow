using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authentication.DTOs;

public class ResendEmailConfirmationRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
