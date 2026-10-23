using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TaskFlow.Entities.Users;

namespace TaskFlow.Repo.AuthorizationRepo;

public interface IAuthorizationRepository
{
    Task<List<ApplicationUser>> GetUsersAsync();
    Task<List<IdentityRole>> GetRolesAsync();
    Task<IdentityRole?> FindRoleAsync(string roleName);
    Task<IdentityResult> CreateRoleAsync(string roleName);
    Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string roleName);
    Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string roleName);
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    Task<IList<Claim>> GetRoleClaimsAsync(IdentityRole role);
    Task<IdentityResult> AddRoleClaimAsync(IdentityRole role, Claim claim);
    Task<IdentityResult> RemoveRoleClaimAsync(IdentityRole role, Claim claim);
    Task<IReadOnlyCollection<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames);
}
