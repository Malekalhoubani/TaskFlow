using Microsoft.AspNetCore.Identity;
using TaskFlow.Entities.Users;
using TaskFlow.Repo.Users.Interfaces;

namespace TaskFlow.Repo.Users.Implementations;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<ApplicationUser?> FindByEmailAsync(string email)
    {
        return _userManager.FindByEmailAsync(email);
    }

    public Task<IdentityResult> CreateAsync(
        ApplicationUser user,
        string password)
    {
        return _userManager.CreateAsync(user, password);
    }

    public async Task<bool> CheckPasswordAsync(
        ApplicationUser user,
        string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public Task<IList<string>> GetRolesAsync(ApplicationUser user)
    {
        return _userManager.GetRolesAsync(user);
    }
}
