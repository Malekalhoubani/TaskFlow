namespace TaskFlow.Service.Email.Options;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string SenderName { get; set; } = "TaskFlow";

    public string SenderEmail { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool UseStartTls { get; set; } = true;

    public string ConfirmationUrl { get; set; } = string.Empty;
    public string ResetPasswordUrl { get; set; } = string.Empty;
    public string ChangeEmailUrl { get; set; } = string.Empty;
}
