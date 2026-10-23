using System.Net;
using TaskFlow.Entities.Security;
using TaskFlow.Entities.Users;
using TaskFlow.Repo.SecurityRepo;
using TaskFlow.Service.Email;
using TaskFlow.Service.Security.DTOs;

namespace TaskFlow.Service.Security;

public class SecurityService : ISecurityService
{
    private readonly ISecurityRepository _securityRepository;
    private readonly IEmailService _emailService;

    public SecurityService(ISecurityRepository securityRepository, IEmailService emailService)
    {
        _securityRepository = securityRepository;
        _emailService = emailService;
    }

    public async Task RecordLoginAsync(ApplicationUser? user, string email, bool succeeded, string? failureReason, string? ipAddress, string? userAgent)
    {
        var previousLogin = user is null ? null : await _securityRepository.GetLastSuccessfulLoginAsync(user.Id);
        await _securityRepository.AddLoginActivityAsync(new LoginActivity
        {
            UserId = user?.Id,
            Email = email,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = Limit(ipAddress, 45),
            UserAgent = Limit(userAgent, 512)
        });
        await _securityRepository.SaveChangesAsync();

        if (succeeded && user?.Email is not null && previousLogin is not null &&
            !string.Equals(previousLogin.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase))
        {
            var safeIp = WebUtility.HtmlEncode(ipAddress ?? "Unknown");
            await _emailService.SendAsync(user.Email, "New TaskFlow sign-in", $"<h2>New sign-in detected</h2><p>Your account was accessed from IP <strong>{safeIp}</strong>.</p><p>If this was not you, change your password and sign out all devices.</p>");
        }
    }

    public async Task<IReadOnlyCollection<LoginActivityDto>> GetLoginHistoryAsync(string userId) =>
        (await _securityRepository.GetRecentLoginActivityAsync(userId, 30)).Select(activity => new LoginActivityDto
        {
            Succeeded = activity.Succeeded,
            FailureReason = activity.FailureReason,
            IpAddress = activity.IpAddress,
            UserAgent = activity.UserAgent,
            CreatedAtUtc = activity.CreatedAtUtc
        }).ToList();

    private static string? Limit(string? value, int length) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= length ? value : value[..length];
}
