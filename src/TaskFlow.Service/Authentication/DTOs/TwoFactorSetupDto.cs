namespace TaskFlow.Service.Authentication.DTOs;

public class TwoFactorSetupDto
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
}
