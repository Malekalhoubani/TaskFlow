using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Service.Authentication;
using TaskFlowAuthenticationService = TaskFlow.Service.Authentication.IAuthenticationService;
using TaskFlow.Service.Authentication.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using TaskFlow.Entities.Users;
using Microsoft.AspNetCore.RateLimiting;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthenticationController : ControllerBase
{
    private readonly TaskFlowAuthenticationService _authenticationService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IConfiguration _configuration;

    public AuthenticationController(
        TaskFlowAuthenticationService authenticationService,
        SignInManager<ApplicationUser> signInManager,
        IAuthenticationSchemeProvider schemeProvider,
        IConfiguration configuration)
    {
        _authenticationService = authenticationService;
        _signInManager = signInManager;
        _schemeProvider = schemeProvider;
        _configuration = configuration;
    }
    private const string RefreshTokenCookieName ="taskflow_refresh_token";

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await _authenticationService.RegisterAsync(request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var ipAddress = HttpContext.Connection
            .RemoteIpAddress?
            .ToString();

        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.LoginAsync(
            request,
            ipAddress,
            userAgent,
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            return Unauthorized(result);
        }

        if (!string.IsNullOrWhiteSpace(result.RefreshToken) &&
            result.RefreshTokenExpiresAtUtc.HasValue)
        {
            SetRefreshTokenCookie(
                result.RefreshToken,
                result.RefreshTokenExpiresAtUtc.Value);
        }

        return Ok(result);
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
    [EnableRateLimiting("login")]
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
    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue(
            RefreshTokenCookieName,
            out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection
            .RemoteIpAddress?
            .ToString();

        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.RefreshTokenAsync(
            refreshToken,
            ipAddress,
            userAgent);

        if (!result.Succeeded ||
            string.IsNullOrWhiteSpace(result.RefreshToken) ||
            !result.RefreshTokenExpiresAtUtc.HasValue)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(result);
        }

        SetRefreshTokenCookie(
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc.Value);

        return Ok(result);
    }
    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = new DateTimeOffset(expiresAtUtc),
                IsEssential = true,
                Path = "/api/auth"
            });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth"
            });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(
            RefreshTokenCookieName,
            out var refreshToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
        {
            var ipAddress = HttpContext.Connection
                .RemoteIpAddress?
                .ToString();

            await _authenticationService.LogoutAsync(
                refreshToken,
                ipAddress);
        }

        DeleteRefreshTokenCookie();

        return Ok(new
        {
            Message = "Logout successful."
        });
    }

    [HttpPost("logout-all-devices")]
    public async Task<IActionResult> LogoutFromAllDevices()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection
            .RemoteIpAddress?
            .ToString();

        var result = await _authenticationService
            .LogoutFromAllDevicesAsync(userId, ipAddress);

        DeleteRefreshTokenCookie();

        return Ok(result);
    }
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var profile = await _authenticationService
            .GetProfileAsync(userId);

        return profile is null
            ? NotFound()
            : Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequestDto request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _authenticationService
            .UpdateProfileAsync(userId, request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail(
        ChangeEmailRequestDto request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _authenticationService
            .ChangeEmailAsync(userId, request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [AllowAnonymous]
    [HttpGet("confirm-email-change")]
    public async Task<IActionResult> ConfirmEmailChange(
        [FromQuery] ConfirmEmailChangeRequestDto request)
    {
        var result = await _authenticationService
            .ConfirmEmailChangeAsync(request);

        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpGet("two-factor/setup")]
    public async Task<IActionResult> GetTwoFactorSetup()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var setup = await _authenticationService.GetTwoFactorSetupAsync(userId);
        return setup is null ? BadRequest() : Ok(setup);
    }

    [HttpPost("two-factor/enable")]
    public async Task<IActionResult> EnableTwoFactor(VerifyTwoFactorRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var recoveryCodes = await _authenticationService.EnableTwoFactorAsync(userId, request);
        return recoveryCodes is null ? BadRequest(new { Message = "Invalid authenticator code." }) : Ok(recoveryCodes);
    }

    [HttpPost("two-factor/disable")]
    public async Task<IActionResult> DisableTwoFactor(VerifyTwoFactorRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var result = await _authenticationService.DisableTwoFactorAsync(userId, request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("two-factor/recovery-codes")]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var codes = await _authenticationService.GenerateRecoveryCodesAsync(userId);
        return codes is null ? BadRequest(new { Message = "Enable two-factor authentication first." }) : Ok(codes);
    }

    [AllowAnonymous]
    [HttpGet("external/{provider}")]
    public async Task<IActionResult> ExternalLogin(string provider)
    {
        if (!string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Microsoft", StringComparison.OrdinalIgnoreCase)) return BadRequest("Unsupported provider.");
        if (await _schemeProvider.GetSchemeAsync(provider) is null) return BadRequest($"{provider} login is not configured.");
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Authentication", new { provider });
        return Challenge(_signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl), provider);
    }

    [AllowAnonymous]
    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string provider)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null) return BadRequest("External login information was not available.");
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("The external provider did not return an email address.");
        var result = await _authenticationService.ExternalLoginAsync(
            provider,
            info.ProviderKey,
            email,
            info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
        if (!result.Succeeded) return BadRequest(result);
        if (!string.IsNullOrWhiteSpace(result.RefreshToken) && result.RefreshTokenExpiresAtUtc.HasValue)
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc.Value);

        var clientUrl = _configuration["ExternalAuthentication:ClientAppUrl"] ?? "https://localhost:4200";
        var origin = new Uri(clientUrl).GetLeftPart(UriPartial.Authority);
        var payload = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var html = $$"""
            <!doctype html><html><body><p>Signing you in to TaskFlow…</p><script>
            if (window.opener) { window.opener.postMessage({ type: 'taskflow-auth', payload: {{payload}} }, {{JsonSerializer.Serialize(origin)}}); window.close(); }
            else { window.location.href = {{JsonSerializer.Serialize(clientUrl)}}; }
            </script></body></html>
            """;
        return Content(html, "text/html");
    }

}
