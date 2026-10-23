using TaskFlow.Entities.Users;
using TaskFlow.Service.Security.DTOs;

namespace TaskFlow.Service.Security;

public interface ISecurityService
{
    Task RecordLoginAsync(ApplicationUser? user, string email, bool succeeded, string? failureReason, string? ipAddress, string? userAgent);
    Task<IReadOnlyCollection<LoginActivityDto>> GetLoginHistoryAsync(string userId);
}
