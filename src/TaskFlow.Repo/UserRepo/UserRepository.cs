using Microsoft.AspNetCore.Identity;
using TaskFlow.Entities.Users;

namespace TaskFlow.Repo.UserRepo;

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

    public Task<ApplicationUser?> FindByIdAsync(string userId)
    {
        return _userManager.FindByIdAsync(userId);
    }

    public Task<IdentityResult> CreateAsync(
        ApplicationUser user,
        string password)
    {
        return _userManager.CreateAsync(user, password);
    }

    public Task<bool> CheckPasswordAsync(
        ApplicationUser user,
        string password)
    {
        return _userManager.CheckPasswordAsync(user, password);
    }

    public Task<IList<string>> GetRolesAsync(ApplicationUser user)
    {
        return _userManager.GetRolesAsync(user);
    }

    public Task<bool> IsEmailConfirmedAsync(ApplicationUser user)
    {
        return _userManager.IsEmailConfirmedAsync(user);
    }

    public Task<string> GenerateEmailConfirmationTokenAsync(
        ApplicationUser user)
    {
        return _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public Task<IdentityResult> ConfirmEmailAsync(
        ApplicationUser user,
        string token)
    {
        return _userManager.ConfirmEmailAsync(user, token);
    }
}
