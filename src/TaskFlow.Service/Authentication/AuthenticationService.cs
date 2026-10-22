using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Entities.Users;
using TaskFlow.Repo.UserRepo;
using TaskFlow.Service.Authentication.Models;
using TaskFlow.Service.Authentication.Options;
using TaskFlow.Service.Email;
using TaskFlow.Service.Email.Options;

namespace TaskFlow.Service.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailSettings _emailSettings;

    public AuthenticationService(
        IUserRepository userRepository,
        IEmailService emailService,
        IOptions<JwtSettings> jwtOptions,
        IOptions<EmailSettings> emailOptions)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _jwtSettings = jwtOptions.Value;
        _emailSettings = emailOptions.Value;
    }

    public async Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request)
    {
        var email = request.Email.Trim();
        var existingUser = await _userRepository.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return Failure("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            UserName = email
        };

        var identityResult = await _userRepository.CreateAsync(
            user,
            request.Password);

        if (!identityResult.Succeeded)
        {
            return new AuthenticationResult
            {
                Succeeded = false,
                Errors = identityResult.Errors
                    .Select(error => error.Description)
            };
        }

        await SendConfirmationEmailAsync(user);

        return new AuthenticationResult
        {
            Succeeded = true,
            Message = "Registration successful. Please confirm your email address.",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await _userRepository.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            return InvalidCredentials();
        }

        var passwordIsValid = await _userRepository.CheckPasswordAsync(
            user,
            request.Password);

        if (!passwordIsValid)
        {
            return InvalidCredentials();
        }

        if (!await _userRepository.IsEmailConfirmedAsync(user))
        {
            return Failure(
                "Please confirm your email address before signing in.");
        }

        var roles = await _userRepository.GetRolesAsync(user);

        return CreateSuccessResult(user, roles);
    }

    public async Task<AuthenticationResult> ConfirmEmailAsync(
        ConfirmEmailRequest request)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId);

        if (user is null)
        {
            return Failure("Invalid email confirmation request.");
        }

        if (await _userRepository.IsEmailConfirmedAsync(user))
        {
            return new AuthenticationResult
            {
                Succeeded = true,
                Message = "Email address is already confirmed."
            };
        }

        var result = await _userRepository.ConfirmEmailAsync(
            user,
            request.Token);

        return result.Succeeded
            ? new AuthenticationResult
            {
                Succeeded = true,
                Message = "Email address confirmed successfully."
            }
            : new AuthenticationResult
            {
                Succeeded = false,
                Errors = result.Errors.Select(error => error.Description)
            };
    }

    public async Task<AuthenticationResult> ResendEmailConfirmationAsync(
        ResendEmailConfirmationRequest request)
    {
        var user = await _userRepository.FindByEmailAsync(
            request.Email.Trim());

        if (user is not null &&
            user.IsActive &&
            !await _userRepository.IsEmailConfirmedAsync(user))
        {
            await SendConfirmationEmailAsync(user);
        }

        return new AuthenticationResult
        {
            Succeeded = true,
            Message = "If the account exists and is not confirmed, a confirmation email has been sent."
        };
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userRepository
            .GenerateEmailConfirmationTokenAsync(user);

        var confirmationLink =
            $"{_emailSettings.ConfirmationUrl}" +
            $"?userId={Uri.EscapeDataString(user.Id)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var safeName = WebUtility.HtmlEncode(user.FullName);
        var safeLink = WebUtility.HtmlEncode(confirmationLink);

        var body = $"""
            <h2>Welcome to TaskFlow, {safeName}</h2>
            <p>Please confirm your email address to activate your account.</p>
            <p><a href="{safeLink}">Confirm email address</a></p>
            <p>If you did not create this account, you can ignore this email.</p>
            """;

        await _emailService.SendAsync(
            user.Email!,
            "Confirm your TaskFlow email address",
            body);
    }

    private AuthenticationResult CreateSuccessResult(
        ApplicationUser user,
        IList<string> roles)
    {
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(
            _jwtSettings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(
            roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Secret));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256));

        return new AuthenticationResult
        {
            Succeeded = true,
            Message = "Login successful.",
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Roles = roles
        };
    }

    private static AuthenticationResult InvalidCredentials()
    {
        return Failure("Invalid email or password.");
    }

    private static AuthenticationResult Failure(string error)
    {
        return new AuthenticationResult
        {
            Succeeded = false,
            Errors = [error]
        };
    }
}
