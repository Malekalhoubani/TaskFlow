using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Service.Authorization;
using TaskFlowAuthorizationService = TaskFlow.Service.Authorization.IAuthorizationService;
using TaskFlow.Service.Authorization.DTOs;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/authorization")]
[Authorize(Policy = Permissions.RolesManage)]
public class AuthorizationController : ControllerBase
{
    private readonly TaskFlowAuthorizationService _authorizationService;

    public AuthorizationController(TaskFlowAuthorizationService authorizationService) => _authorizationService = authorizationService;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() => Ok(await _authorizationService.GetUsersAsync());

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles() => Ok(await _authorizationService.GetRolesAsync());

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(CreateRoleRequestDto request)
    {
        var result = await _authorizationService.CreateRoleAsync(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("users/assign-role")]
    public async Task<IActionResult> AssignRole(AssignRoleRequestDto request)
    {
        var result = await _authorizationService.AssignRoleAsync(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("users/remove-role")]
    public async Task<IActionResult> RemoveRole(AssignRoleRequestDto request)
    {
        var result = await _authorizationService.RemoveRoleAsync(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("roles/permissions")]
    public async Task<IActionResult> UpdatePermissions(UpdateRolePermissionsRequestDto request)
    {
        var result = await _authorizationService.UpdatePermissionsAsync(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
