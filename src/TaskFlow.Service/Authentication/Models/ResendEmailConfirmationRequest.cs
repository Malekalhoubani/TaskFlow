using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authentication.Models;

public class ResendEmailConfirmationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}