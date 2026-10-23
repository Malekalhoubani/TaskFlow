using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Entities.Users;
using TaskFlow.Repo.UserRepo;
using TaskFlow.Service.Authentication.DTOs;
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

    public async Task<AuthenticationResultDto> RegisterAsync(
        RegisterRequestDto request)
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
            return new AuthenticationResultDto
            {
                Succeeded = false,
                Errors = identityResult.Errors
                    .Select(error => error.Description)
            };
        }

        await SendConfirmationEmailAsync(user);

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "Registration successful. Please confirm your email address.",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task<AuthenticationResultDto> LoginAsync(
        LoginRequestDto request)
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

    public async Task<AuthenticationResultDto> ConfirmEmailAsync(
        ConfirmEmailRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId);

        if (user is null)
        {
            return Failure("Invalid email confirmation request.");
        }

        if (await _userRepository.IsEmailConfirmedAsync(user))
        {
            return new AuthenticationResultDto
            {
                Succeeded = true,
                Message = "Email address is already confirmed."
            };
        }

        var result = await _userRepository.ConfirmEmailAsync(
            user,
            request.Token);

        return result.Succeeded
            ? new AuthenticationResultDto
            {
                Succeeded = true,
                Message = "Email address confirmed successfully."
            }
            : new AuthenticationResultDto
            {
                Succeeded = false,
                Errors = result.Errors.Select(error => error.Description)
            };
    }

    public async Task<AuthenticationResultDto> ResendEmailConfirmationAsync(
        ResendEmailConfirmationRequestDto request)
    {
        var user = await _userRepository.FindByEmailAsync(
            request.Email.Trim());

        if (user is not null &&
            user.IsActive &&
            !await _userRepository.IsEmailConfirmedAsync(user))
        {
            await SendConfirmationEmailAsync(user);
        }

        return new AuthenticationResultDto
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

    private AuthenticationResultDto CreateSuccessResult(
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

        return new AuthenticationResultDto
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

    private static AuthenticationResultDto InvalidCredentials()
    {
        return Failure("Invalid email or password.");
    }

    private static AuthenticationResultDto Failure(string error)
    {
        return new AuthenticationResultDto
        {
            Succeeded = false,
            Errors = [error]
        };
    }

    public async Task<AuthenticationResultDto> ForgotPasswordAsync(
        ForgotPasswordRequestDto request)
    {
        var email = request.Email.Trim();
        var user = await _userRepository.FindByEmailAsync(email);

        if (user is not null && user.IsActive)
        {
            var token = await _userRepository
                .GeneratePasswordResetTokenAsync(user);

            var resetLink =
                $"{_emailSettings.ResetPasswordUrl}" +
                $"?email={Uri.EscapeDataString(email)}" +
                $"&token={Uri.EscapeDataString(token)}";

            var safeName = WebUtility.HtmlEncode(user.FullName);
            var safeLink = WebUtility.HtmlEncode(resetLink);

            var body = $"""
            <h2>Password reset request</h2>
            <p>Hello {safeName},</p>
            <p>Click the link below to reset your TaskFlow password.</p>
            <p><a href="{safeLink}">Reset password</a></p>
            <p>If you did not request this change, you can ignore this email.</p>
            """;

            await _emailService.SendAsync(
                user.Email!,
                "Reset your TaskFlow password",
                body);
        }

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "If an account exists for this email, a password reset link has been sent."
        };
    }
    public async Task<AuthenticationResultDto> ResetPasswordAsync(
        ResetPasswordRequestDto request)
    {
        var user = await _userRepository.FindByEmailAsync(
            request.Email.Trim());

        if (user is null || !user.IsActive)
        {
            return Failure("Invalid password reset request.");
        }

        var result = await _userRepository.ResetPasswordAsync(
            user,
            request.Token,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return new AuthenticationResultDto
            {
                Succeeded = false,
                Errors = result.Errors.Select(
                    error => error.Description)
            };
        }

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "Password reset successfully. You can now sign in."
        };
    }
    public async Task<AuthenticationResultDto> ChangePasswordAsync(
        string userId,
        ChangePasswordRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            return Failure("User account was not found.");
        }

        var result = await _userRepository.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return new AuthenticationResultDto
            {
                Succeeded = false,
                Errors = result.Errors.Select(
                    error => error.Description)
            };
        }

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "Password changed successfully."
        };
    }

}
