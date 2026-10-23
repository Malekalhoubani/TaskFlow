using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authentication.DTOs;

public class VerifyTwoFactorRequestDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
}
