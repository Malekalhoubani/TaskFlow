using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authentication.DTOs;

public class ConfirmEmailChangeRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string NewEmail { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}