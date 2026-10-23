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
using TaskFlow.Entities.Authentication;
using TaskFlow.Repo.RefreshTokenRepo;
using TaskFlow.Service.Authentication.Tokens;
using TaskFlow.Repo.AuthorizationRepo;
using TaskFlow.Service.Authorization;
using TaskFlow.Service.Security;
using Microsoft.AspNetCore.Identity;
namespace TaskFlow.Service.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailSettings _emailSettings;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly ISecurityService _securityService;
    public AuthenticationService(
     IUserRepository userRepository,
     IRefreshTokenRepository refreshTokenRepository,
     ITokenService tokenService,
     IAuthorizationRepository authorizationRepository,
     ISecurityService securityService,
     IEmailService emailService,
     IOptions<JwtSettings> jwtOptions,
     IOptions<EmailSettings> emailOptions)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _authorizationRepository = authorizationRepository;
        _securityService = securityService;
        _emailService = emailService;
        _jwtSettings = jwtOptions.Value;
        _emailSettings = emailOptions.Value;
    }

    public async Task<AuthenticationResultDto> RegisterAsync(RegisterRequestDto request)
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

    public async Task<AuthenticationResultDto> LoginAsync(LoginRequestDto request , string?ipAddress , string?userAgent , CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await _userRepository.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            await _securityService.RecordLoginAsync(user, email, false, "Invalid credentials.", ipAddress, userAgent);
            return InvalidCredentials();
        }

        if (await _userRepository.IsLockedOutAsync(user))
        {
            await _securityService.RecordLoginAsync(user, email, false, "Account locked.", ipAddress, userAgent);
            return Failure("Account is temporarily locked. Please try again later.");
        }

        var passwordIsValid = await _userRepository.CheckPasswordAsync(
            user,
            request.Password);

        if (!passwordIsValid)
        {
            await _userRepository.AccessFailedAsync(user);
            await _securityService.RecordLoginAsync(user, email, false, "Invalid credentials.", ipAddress, userAgent);
            return InvalidCredentials();
        }

        if (!await _userRepository.IsEmailConfirmedAsync(user))
        {
            return Failure(
                "Please confirm your email address before signing in.");
        }

        if (await _userRepository.GetTwoFactorEnabledAsync(user))
        {
            var twoFactorIsValid = !string.IsNullOrWhiteSpace(request.RecoveryCode)
                ? (await _userRepository.RedeemRecoveryCodeAsync(user, request.RecoveryCode.Replace(" ", string.Empty))).Succeeded
                : !string.IsNullOrWhiteSpace(request.TwoFactorCode) &&
                  await _userRepository.VerifyTwoFactorTokenAsync(user, request.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty));

            if (!twoFactorIsValid)
            {
                return new AuthenticationResultDto
                {
                    Succeeded = false,
                    RequiresTwoFactor = true,
                    Errors = ["A valid authenticator or recovery code is required."]
                };
            }
        }

        await _userRepository.ResetAccessFailedCountAsync(user);

        var roles = await _userRepository.GetRolesAsync(user);
        var permissions = await _authorizationRepository.GetPermissionsForRolesAsync(roles);

        var authenticationResult = CreateSuccessResult(user, roles, permissions);

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(rawRefreshToken);
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(
            _jwtSettings.RefreshTokenExpirationDays);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            FamilyId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
            CreatedByIp = ipAddress,
            UserAgent = Truncate(userAgent, 512)
        };

        await _refreshTokenRepository.AddAsync(
            refreshToken,
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(
            cancellationToken);

        authenticationResult.RefreshToken = rawRefreshToken;
        authenticationResult.RefreshTokenExpiresAtUtc =
            refreshTokenExpiresAtUtc;

        await _securityService.RecordLoginAsync(user, email, true, null, ipAddress, userAgent);

        return authenticationResult;
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
        IList<string> roles,
        IEnumerable<string> permissions)
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
        claims.AddRange(
            permissions.Select(permission => new Claim(Permissions.ClaimType, permission)));

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
    private static string? Truncate(string? value,int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();

        return trimmedValue.Length <= maximumLength
            ? trimmedValue
            : trimmedValue[..maximumLength];
    }

    public async Task<AuthenticationResultDto> RefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _refreshTokenRepository.FindByTokenHashAsync(tokenHash);

        if (storedToken is null)
        {
            return Failure("Invalid refresh token.");
        }

        if (storedToken.RevokedAtUtc.HasValue)
        {
            var activeFamilyTokens =
                await _refreshTokenRepository.GetActiveByFamilyIdAsync(storedToken.FamilyId);

            foreach (var familyToken in activeFamilyTokens)
            {
                familyToken.RevokedAtUtc = DateTime.UtcNow;
                familyToken.RevokedByIp = ipAddress;
                familyToken.RevocationReason = "Refresh token reuse detected.";
            }

            await _refreshTokenRepository.SaveChangesAsync();

            return Failure(
                "This refresh token has already been used.");
        }

        if (storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Failure("Refresh token has expired.");
        }

        if (!storedToken.User.IsActive)
        {
            return Failure("User account is inactive.");
        }

        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash =
            _tokenService.HashToken(newRawRefreshToken);

        var newRefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(
            _jwtSettings.RefreshTokenExpirationDays);

        storedToken.LastUsedAtUtc = DateTime.UtcNow;
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;
        storedToken.RevocationReason = "Rotated.";

        var newRefreshToken = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            FamilyId = storedToken.FamilyId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = newRefreshTokenExpiresAtUtc,
            CreatedByIp = ipAddress,
            UserAgent = Truncate(userAgent, 512)
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        var roles = await _userRepository.GetRolesAsync(storedToken.User);
        var permissions = await _authorizationRepository.GetPermissionsForRolesAsync(roles);
        var result = CreateSuccessResult(storedToken.User, roles, permissions);

        result.RefreshToken = newRawRefreshToken;
        result.RefreshTokenExpiresAtUtc = newRefreshTokenExpiresAtUtc;

        return result;
    }
    public async Task<AuthenticationResultDto> LogoutAsync(string refreshToken, string? ipAddress)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        var storedToken = await _refreshTokenRepository
            .FindByTokenHashAsync(tokenHash);

        if (storedToken is not null &&
            storedToken.RevokedAtUtc is null)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;
            storedToken.RevocationReason = "User logged out.";

            await _refreshTokenRepository.SaveChangesAsync();
        }

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "Logout successful."
        };
    }

    public async Task<AuthenticationResultDto> LogoutFromAllDevicesAsync(string userId, string? ipAddress)
    {
        var activeTokens = await _refreshTokenRepository
            .GetActiveByUserIdAsync(userId);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevocationReason = "User logged out from all devices.";
        }

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "Logout from all devices successful."
        };
    }
    public async Task<UserProfileDto?> GetProfileAsync(string userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        return new UserProfileDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    public async Task<AuthenticationResultDto> UpdateProfileAsync(
        string userId,
        UpdateProfileRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            return Failure("User account was not found.");
        }

        user.FullName = request.FullName.Trim();

        var updateResult = await _userRepository.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return new AuthenticationResultDto
            {
                Succeeded = false,
                Errors = updateResult.Errors.Select(
                    error => error.Description)
            };
        }

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "Profile updated successfully.",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task<AuthenticationResultDto> ChangeEmailAsync(
    string userId,
    ChangeEmailRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            return Failure("User account was not found.");
        }

        var newEmail = request.NewEmail.Trim();

        if (string.Equals(
            user.Email,
            newEmail,
            StringComparison.OrdinalIgnoreCase))
        {
            return Failure("The new email is the same as the current email.");
        }

        var passwordIsValid = await _userRepository.CheckPasswordAsync(
            user,
            request.CurrentPassword);

        if (!passwordIsValid)
        {
            return Failure("Current password is incorrect.");
        }

        var existingUser = await _userRepository.FindByEmailAsync(newEmail);

        if (existingUser is not null)
        {
            return Failure("Email is already registered.");
        }

        var token = await _userRepository
            .GenerateChangeEmailTokenAsync(user, newEmail);

        var confirmationLink =
            $"{_emailSettings.ChangeEmailUrl}" +
            $"?userId={Uri.EscapeDataString(user.Id)}" +
            $"&newEmail={Uri.EscapeDataString(newEmail)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var safeName = WebUtility.HtmlEncode(user.FullName);
        var safeLink = WebUtility.HtmlEncode(confirmationLink);

        var body = $"""
        <h2>Confirm your new email address</h2>
        <p>Hello {safeName},</p>
        <p>Click the link below to confirm your new TaskFlow email.</p>
        <p><a href="{safeLink}">Confirm new email address</a></p>
        <p>If you did not request this change, secure your account immediately.</p>
        """;

        await _emailService.SendAsync(
            newEmail,
            "Confirm your new TaskFlow email address",
            body);

        return new AuthenticationResultDto
        {
            Succeeded = true,
            Message = "A confirmation link has been sent to the new email address."
        };
    }

    public async Task<AuthenticationResultDto> ConfirmEmailChangeAsync(
        ConfirmEmailChangeRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId);

        if (user is null || !user.IsActive)
        {
            return Failure("Invalid email change request.");
        }

        var newEmail = request.NewEmail.Trim();

        var result = await _userRepository.ChangeEmailAsync(
            user,
            newEmail,
            request.Token);

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
            Message = "Email address changed successfully.",
            UserId = user.Id,
            FullName = user.FullName,
            Email = newEmail
        };
    }

    public async Task<TwoFactorSetupDto?> GetTwoFactorSetupAsync(string userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user is null || !user.IsActive) return null;

        var key = await _userRepository.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _userRepository.ResetAuthenticatorKeyAsync(user);
            key = await _userRepository.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrWhiteSpace(key)) return null;
        var issuer = Uri.EscapeDataString(_jwtSettings.Issuer);
        var account = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id);
        return new TwoFactorSetupDto
        {
            SharedKey = key,
            AuthenticatorUri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6"
        };
    }

    public async Task<RecoveryCodesDto?> EnableTwoFactorAsync(string userId, VerifyTwoFactorRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user is null || !await _userRepository.VerifyTwoFactorTokenAsync(user, request.Code.Replace(" ", string.Empty).Replace("-", string.Empty))) return null;
        if (!(await _userRepository.SetTwoFactorEnabledAsync(user, true)).Succeeded) return null;
        return new RecoveryCodesDto { Codes = await _userRepository.GenerateRecoveryCodesAsync(user, 10) ?? [] };
    }

    public async Task<AuthenticationResultDto> DisableTwoFactorAsync(string userId, VerifyTwoFactorRequestDto request)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user is null || !await _userRepository.VerifyTwoFactorTokenAsync(user, request.Code.Replace(" ", string.Empty).Replace("-", string.Empty)))
            return Failure("Invalid authenticator code.");
        var result = await _userRepository.SetTwoFactorEnabledAsync(user, false);
        return result.Succeeded ? new AuthenticationResultDto { Succeeded = true, Message = "Two-factor authentication disabled." } : Failure("Unable to disable two-factor authentication.");
    }

    public async Task<RecoveryCodesDto?> GenerateRecoveryCodesAsync(string userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user is null || !await _userRepository.GetTwoFactorEnabledAsync(user)) return null;
        return new RecoveryCodesDto { Codes = await _userRepository.GenerateRecoveryCodesAsync(user, 10) ?? [] };
    }

    public async Task<AuthenticationResultDto> ExternalLoginAsync(string provider, string providerKey, string email, string fullName, string? ipAddress, string? userAgent)
    {
        var user = await _userRepository.FindByLoginAsync(provider, providerKey) ?? await _userRepository.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName.Trim()
            };
            var createResult = await _userRepository.CreateAsync(user);
            if (!createResult.Succeeded) return new AuthenticationResultDto { Succeeded = false, Errors = createResult.Errors.Select(error => error.Description) };
        }

        if (await _userRepository.FindByLoginAsync(provider, providerKey) is null)
        {
            var linkResult = await _userRepository.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
            if (!linkResult.Succeeded) return new AuthenticationResultDto { Succeeded = false, Errors = linkResult.Errors.Select(error => error.Description) };
        }

        if (!user.IsActive) return Failure("User account is inactive.");
        var roles = await _userRepository.GetRolesAsync(user);
        var permissions = await _authorizationRepository.GetPermissionsForRolesAsync(roles);
        var result = CreateSuccessResult(user, roles, permissions);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(rawRefreshToken),
            FamilyId = Guid.NewGuid(),
            ExpiresAtUtc = refreshTokenExpiry,
            CreatedByIp = ipAddress,
            UserAgent = Truncate(userAgent, 512)
        });
        await _refreshTokenRepository.SaveChangesAsync();
        result.RefreshToken = rawRefreshToken;
        result.RefreshTokenExpiresAtUtc = refreshTokenExpiry;
        await _securityService.RecordLoginAsync(user, email, true, null, ipAddress, userAgent);
        return result;
    }
}
