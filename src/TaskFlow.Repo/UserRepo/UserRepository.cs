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
    public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
    {
        return _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public Task<IdentityResult> ResetPasswordAsync(ApplicationUser user,
        string token,
        string newPassword)
    {
        return _userManager.ResetPasswordAsync(
            user,
            token,
            newPassword);
    }

    public Task<IdentityResult> ChangePasswordAsync(
        ApplicationUser user,
        string currentPassword,
        string newPassword)
    {
        return _userManager.ChangePasswordAsync(
            user,
            currentPassword,
            newPassword);
    }

}
