using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Service.Security;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/security")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly ISecurityService _securityService;
    public SecurityController(ISecurityService securityService) => _securityService = securityService;

    [HttpGet("login-history")]
    public async Task<IActionResult> GetLoginHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? Unauthorized() : Ok(await _securityService.GetLoginHistoryAsync(userId));
    }
}
