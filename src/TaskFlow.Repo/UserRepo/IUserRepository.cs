using Microsoft.AspNetCore.Identity;
using TaskFlow.Entities.Users;

namespace TaskFlow.Repo.UserRepo;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(string email);

    Task<ApplicationUser?> FindByIdAsync(string userId);

    Task<IdentityResult> CreateAsync(
        ApplicationUser user,
        string password);

    Task<bool> CheckPasswordAsync(
        ApplicationUser user,
        string password);

    Task<IList<string>> GetRolesAsync(ApplicationUser user);

    Task<bool> IsEmailConfirmedAsync(ApplicationUser user);

    Task<string> GenerateEmailConfirmationTokenAsync(
        ApplicationUser user);

    Task<IdentityResult> ConfirmEmailAsync(
        ApplicationUser user,
        string token);
}
