using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TaskFlow.Entities.Users;

namespace TaskFlow.Repo.UserRepo;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(string email);

    Task<ApplicationUser?> FindByIdAsync(string userId);

    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);

    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

    Task<IList<string>> GetRolesAsync(ApplicationUser user);

    Task<bool> IsEmailConfirmedAsync(ApplicationUser user);

    Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);

    Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token);

    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);

    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
    Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);

    Task<IdentityResult> UpdateAsync(ApplicationUser user);
    Task<string> GenerateChangeEmailTokenAsync(ApplicationUser user, string newEmail);
    Task<IdentityResult> ChangeEmailAsync(ApplicationUser user, string newEmail, string token);
    Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user);
    Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user);
    Task<IdentityResult> ResetAuthenticatorKeyAsync(ApplicationUser user);
    Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string code);
    Task<IdentityResult> SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled);
    Task<IEnumerable<string>?> GenerateRecoveryCodesAsync(ApplicationUser user, int count);
    Task<IdentityResult> RedeemRecoveryCodeAsync(ApplicationUser user, string code);
    Task<IList<Claim>> GetClaimsAsync(ApplicationUser user);
    Task<bool> IsLockedOutAsync(ApplicationUser user);
    Task<IdentityResult> AccessFailedAsync(ApplicationUser user);
    Task<IdentityResult> ResetAccessFailedCountAsync(ApplicationUser user);
    Task<ApplicationUser?> FindByLoginAsync(string provider, string providerKey);
    Task<IdentityResult> CreateAsync(ApplicationUser user);
    Task<IdentityResult> AddLoginAsync(ApplicationUser user, UserLoginInfo loginInfo);
}
