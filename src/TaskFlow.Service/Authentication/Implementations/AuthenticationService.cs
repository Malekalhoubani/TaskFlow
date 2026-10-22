using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Entities.Users;
using TaskFlow.Repo.Users.Interfaces;
using TaskFlow.Service.Authentication.Interfaces;
using TaskFlow.Service.Authentication.Models;
using TaskFlow.Service.Authentication.Options;

namespace TaskFlow.Service.Authentication.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationService(
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtOptions)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request)
    {
        var email = request.Email.Trim();
        var existingUser = await _userRepository.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return new AuthenticationResult
            {
                Succeeded = false,
                Errors = ["Email is already registered."]
            };
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

        return new AuthenticationResult
        {
            Succeeded = identityResult.Succeeded,
            Errors = identityResult.Errors
                .Select(error => error.Description)
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

        var roles = await _userRepository.GetRolesAsync(user);

        return CreateSuccessResult(user, roles);
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
        return new AuthenticationResult
        {
            Succeeded = false,
            Errors = ["Invalid email or password."]
        };
    }
}
