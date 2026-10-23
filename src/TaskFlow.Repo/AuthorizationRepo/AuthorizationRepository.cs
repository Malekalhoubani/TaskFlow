using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Entities.Users;

namespace TaskFlow.Repo.AuthorizationRepo;

public class AuthorizationRepository : IAuthorizationRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthorizationRepository(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public Task<List<ApplicationUser>> GetUsersAsync() => _userManager.Users.OrderBy(user => user.Email).ToListAsync();
    public Task<List<IdentityRole>> GetRolesAsync() => _roleManager.Roles.OrderBy(role => role.Name).ToListAsync();
    public Task<IdentityRole?> FindRoleAsync(string roleName) => _roleManager.FindByNameAsync(roleName);
    public Task<IdentityResult> CreateRoleAsync(string roleName) => _roleManager.CreateAsync(new IdentityRole(roleName));
    public Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string roleName) => _userManager.AddToRoleAsync(user, roleName);
    public Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string roleName) => _userManager.RemoveFromRoleAsync(user, roleName);
    public Task<IList<string>> GetUserRolesAsync(ApplicationUser user) => _userManager.GetRolesAsync(user);
    public Task<IList<Claim>> GetRoleClaimsAsync(IdentityRole role) => _roleManager.GetClaimsAsync(role);
    public Task<IdentityResult> AddRoleClaimAsync(IdentityRole role, Claim claim) => _roleManager.AddClaimAsync(role, claim);
    public Task<IdentityResult> RemoveRoleClaimAsync(IdentityRole role, Claim claim) => _roleManager.RemoveClaimAsync(role, claim);

    public async Task<IReadOnlyCollection<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            foreach (var claim in await _roleManager.GetClaimsAsync(role))
            {
                if (claim.Type == "permission") permissions.Add(claim.Value);
            }
        }
        return permissions;
    }
}
