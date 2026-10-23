using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
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
    public Task<IdentityResult> UpdateAsync(ApplicationUser user)
    {
        return _userManager.UpdateAsync(user);
    }

    public Task<string> GenerateChangeEmailTokenAsync(
        ApplicationUser user,
        string newEmail)
    {
        return _userManager.GenerateChangeEmailTokenAsync(
            user,
            newEmail);
    }

    public async Task<IdentityResult> ChangeEmailAsync(
        ApplicationUser user,
        string newEmail,
        string token)
    {
        var emailResult = await _userManager.ChangeEmailAsync(
            user,
            newEmail,
            token);

        if (!emailResult.Succeeded)
        {
            return emailResult;
        }

        return await _userManager.SetUserNameAsync(
            user,
            newEmail);
    }

    public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user) => _userManager.GetTwoFactorEnabledAsync(user);
    public Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user) => _userManager.GetAuthenticatorKeyAsync(user);
    public Task<IdentityResult> ResetAuthenticatorKeyAsync(ApplicationUser user) => _userManager.ResetAuthenticatorKeyAsync(user);
    public Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string code) =>
        _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);
    public Task<IdentityResult> SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled) => _userManager.SetTwoFactorEnabledAsync(user, enabled);
    public Task<IEnumerable<string>?> GenerateRecoveryCodesAsync(ApplicationUser user, int count) => _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, count);
    public Task<IdentityResult> RedeemRecoveryCodeAsync(ApplicationUser user, string code) => _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
    public Task<IList<Claim>> GetClaimsAsync(ApplicationUser user) => _userManager.GetClaimsAsync(user);
    public Task<bool> IsLockedOutAsync(ApplicationUser user) => _userManager.IsLockedOutAsync(user);
    public Task<IdentityResult> AccessFailedAsync(ApplicationUser user) => _userManager.AccessFailedAsync(user);
    public Task<IdentityResult> ResetAccessFailedCountAsync(ApplicationUser user) => _userManager.ResetAccessFailedCountAsync(user);
    public Task<ApplicationUser?> FindByLoginAsync(string provider, string providerKey) => _userManager.FindByLoginAsync(provider, providerKey);
    public Task<IdentityResult> CreateAsync(ApplicationUser user) => _userManager.CreateAsync(user);
    public Task<IdentityResult> AddLoginAsync(ApplicationUser user, UserLoginInfo loginInfo) => _userManager.AddLoginAsync(user, loginInfo);
}
