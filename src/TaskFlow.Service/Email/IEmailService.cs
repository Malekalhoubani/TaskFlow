namespace TaskFlow.Service.Email;

public interface IEmailService
{
    Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
