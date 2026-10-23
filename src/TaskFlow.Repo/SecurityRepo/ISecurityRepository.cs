using TaskFlow.Entities.Security;

namespace TaskFlow.Repo.SecurityRepo;

public interface ISecurityRepository
{
    Task AddLoginActivityAsync(LoginActivity activity);
    Task<LoginActivity?> GetLastSuccessfulLoginAsync(string userId);
    Task<List<LoginActivity>> GetRecentLoginActivityAsync(string userId, int count);
    Task SaveChangesAsync();
}
