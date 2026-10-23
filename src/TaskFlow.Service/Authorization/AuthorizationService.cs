using System.Security.Claims;
using TaskFlow.Repo.AuthorizationRepo;
using TaskFlow.Repo.UserRepo;
using TaskFlow.Service.Authentication.DTOs;
using TaskFlow.Service.Authorization.DTOs;

namespace TaskFlow.Service.Authorization;

public class AuthorizationService : IAuthorizationService
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IUserRepository _userRepository;

    public AuthorizationService(IAuthorizationRepository authorizationRepository, IUserRepository userRepository)
    {
        _authorizationRepository = authorizationRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync()
    {
        var users = await _authorizationRepository.GetUsersAsync();
        var results = new List<UserSummaryDto>();

        foreach (var user in users)
        {
            results.Add(new UserSummaryDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                IsActive = user.IsActive,
                Roles = await _authorizationRepository.GetUserRolesAsync(user)
            });
        }

        return results;
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetRolesAsync()
    {
        var roles = await _authorizationRepository.GetRolesAsync();
        var results = new List<RoleDto>();

        foreach (var role in roles)
        {
            var claims = await _authorizationRepository.GetRoleClaimsAsync(role);
            results.Add(new RoleDto
            {
                Name = role.Name ?? string.Empty,
                Permissions = claims.Where(claim => claim.Type == Permissions.ClaimType).Select(claim => claim.Value)
            });
        }

        return results;
    }

    public async Task<AuthenticationResultDto> CreateRoleAsync(CreateRoleRequestDto request)
    {
        var roleName = request.RoleName.Trim();
        if (await _authorizationRepository.FindRoleAsync(roleName) is not null)
        {
            return Failure("Role already exists.");
        }

        return FromIdentityResult(await _authorizationRepository.CreateRoleAsync(roleName), "Role created successfully.");
    }

    public async Task<AuthenticationResultDto> AssignRoleAsync(AssignRoleRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId);
        if (user is null || await _authorizationRepository.FindRoleAsync(request.RoleName) is null)
        {
            return Failure("User or role was not found.");
        }

        return FromIdentityResult(await _authorizationRepository.AddToRoleAsync(user, request.RoleName), "Role assigned successfully.");
    }

    public async Task<AuthenticationResultDto> RemoveRoleAsync(AssignRoleRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return Failure("User was not found.");
        }

        return FromIdentityResult(await _authorizationRepository.RemoveFromRoleAsync(user, request.RoleName), "Role removed successfully.");
    }

    public async Task<AuthenticationResultDto> UpdatePermissionsAsync(UpdateRolePermissionsRequestDto request)
    {
        var role = await _authorizationRepository.FindRoleAsync(request.RoleName);
        if (role is null)
        {
            return Failure("Role was not found.");
        }

        var requested = request.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requested.Any(permission => !Permissions.All.Contains(permission, StringComparer.OrdinalIgnoreCase)))
        {
            return Failure("One or more permissions are invalid.");
        }

        var currentClaims = (await _authorizationRepository.GetRoleClaimsAsync(role))
            .Where(claim => claim.Type == Permissions.ClaimType)
            .ToList();

        foreach (var claim in currentClaims.Where(claim => !requested.Contains(claim.Value)))
        {
            await _authorizationRepository.RemoveRoleClaimAsync(role, claim);
        }

        foreach (var permission in requested.Where(permission => currentClaims.All(claim => !string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase))))
        {
            await _authorizationRepository.AddRoleClaimAsync(role, new Claim(Permissions.ClaimType, permission));
        }

        return Success("Role permissions updated successfully.");
    }

    private static AuthenticationResultDto FromIdentityResult(Microsoft.AspNetCore.Identity.IdentityResult result, string message) =>
        result.Succeeded ? Success(message) : new AuthenticationResultDto { Succeeded = false, Errors = result.Errors.Select(error => error.Description) };

    private static AuthenticationResultDto Success(string message) => new() { Succeeded = true, Message = message };
    private static AuthenticationResultDto Failure(string error) => new() { Succeeded = false, Errors = [error] };
}
