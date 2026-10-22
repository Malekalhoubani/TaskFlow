using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Service.Authentication.Models;

public class ConfirmEmailRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}