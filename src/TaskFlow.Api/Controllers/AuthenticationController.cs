using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Service.Authentication;
using TaskFlow.Service.Authentication.Models;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authenticationService.RegisterAsync(request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authenticationService.LoginAsync(request);

        return result.Succeeded
            ? Ok(result)
            : Unauthorized(result);
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] ConfirmEmailRequest request)
    {
        var result = await _authenticationService.ConfirmEmailAsync(request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendEmailConfirmation(
        ResendEmailConfirmationRequest request)
    {
        var result = await _authenticationService
            .ResendEmailConfirmationAsync(request);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Name = User.Identity?.Name,
            Email = User.FindFirstValue(ClaimTypes.Email),
            Roles = User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
        });
    }
}
