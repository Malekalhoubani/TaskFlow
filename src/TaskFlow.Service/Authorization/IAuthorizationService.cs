using TaskFlow.Service.Authentication.DTOs;
using TaskFlow.Service.Authorization.DTOs;

namespace TaskFlow.Service.Authorization;

public interface IAuthorizationService
{
    Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync();
    Task<IReadOnlyCollection<RoleDto>> GetRolesAsync();
    Task<AuthenticationResultDto> CreateRoleAsync(CreateRoleRequestDto request);
    Task<AuthenticationResultDto> AssignRoleAsync(AssignRoleRequestDto request);
    Task<AuthenticationResultDto> RemoveRoleAsync(AssignRoleRequestDto request);
    Task<AuthenticationResultDto> UpdatePermissionsAsync(UpdateRolePermissionsRequestDto request);
}
