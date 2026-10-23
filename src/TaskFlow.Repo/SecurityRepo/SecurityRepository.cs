using Microsoft.EntityFrameworkCore;
using TaskFlow.Entities.Security;
using TaskFlow.Repo.Data;

namespace TaskFlow.Repo.SecurityRepo;

public class SecurityRepository : ISecurityRepository
{
    private readonly ApplicationDbContext _dbContext;
    public SecurityRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task AddLoginActivityAsync(LoginActivity activity) => _dbContext.LoginActivities.AddAsync(activity).AsTask();
    public Task<LoginActivity?> GetLastSuccessfulLoginAsync(string userId) => _dbContext.LoginActivities
        .Where(activity => activity.UserId == userId && activity.Succeeded)
        .OrderByDescending(activity => activity.CreatedAtUtc)
        .FirstOrDefaultAsync();
    public Task<List<LoginActivity>> GetRecentLoginActivityAsync(string userId, int count) => _dbContext.LoginActivities
        .Where(activity => activity.UserId == userId)
        .OrderByDescending(activity => activity.CreatedAtUtc)
        .Take(count)
        .AsNoTracking()
        .ToListAsync();
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
