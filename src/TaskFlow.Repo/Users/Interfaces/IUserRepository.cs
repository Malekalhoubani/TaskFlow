using Microsoft.AspNetCore.Identity;
using TaskFlow.Entities.Users;

namespace TaskFlow.Repo.Users.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(string email);

    Task<IdentityResult> CreateAsync(
        ApplicationUser user,
        string password);

    Task<bool> CheckPasswordAsync(
        ApplicationUser user,
        string password);

    Task<IList<string>> GetRolesAsync(ApplicationUser user);
}
