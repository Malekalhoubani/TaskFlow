using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Service.Authentication;
using TaskFlow.Service.Authentication.DTOs;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
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
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await _authenticationService.RegisterAsync(request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await _authenticationService.LoginAsync(request);

        return result.Succeeded
            ? Ok(result)
            : Unauthorized(result);
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] ConfirmEmailRequestDto request)
    {
        var result = await _authenticationService.ConfirmEmailAsync(request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendEmailConfirmation(
        ResendEmailConfirmationRequestDto request)
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

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequestDto request)
    {
        var result = await _authenticationService.ForgotPasswordAsync(request);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequestDto request)
    {
        var result = await _authenticationService.ResetPasswordAsync(request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequestDto request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _authenticationService
            .ChangePasswordAsync(userId, request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }
}
