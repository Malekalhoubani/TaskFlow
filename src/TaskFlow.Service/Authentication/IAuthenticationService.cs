using TaskFlow.Service.Authentication.DTOs;

namespace TaskFlow.Service.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResultDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthenticationResultDto> LoginAsync(LoginRequestDto request , string? ipAddress , string?userAgent , CancellationToken cancellationToken);
    Task<AuthenticationResultDto> ConfirmEmailAsync(ConfirmEmailRequestDto request);
    Task<AuthenticationResultDto> ResendEmailConfirmationAsync(ResendEmailConfirmationRequestDto request);
    Task<AuthenticationResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<AuthenticationResultDto> ResetPasswordAsync(ResetPasswordRequestDto request);

    Task<AuthenticationResultDto> ChangePasswordAsync(string userId,ChangePasswordRequestDto request);
    Task<AuthenticationResultDto> RefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent);
    Task<AuthenticationResultDto> LogoutAsync(string refreshToken, string? ipAddress);
    Task<AuthenticationResultDto> LogoutFromAllDevicesAsync(string userId, string? ipAddress);
    Task<UserProfileDto?> GetProfileAsync(string userId);
    Task<AuthenticationResultDto> UpdateProfileAsync(string userId, UpdateProfileRequestDto request);
    Task<AuthenticationResultDto> ChangeEmailAsync(string userId, ChangeEmailRequestDto request);
    Task<AuthenticationResultDto> ConfirmEmailChangeAsync(ConfirmEmailChangeRequestDto request);
    Task<TwoFactorSetupDto?> GetTwoFactorSetupAsync(string userId);
    Task<RecoveryCodesDto?> EnableTwoFactorAsync(string userId, VerifyTwoFactorRequestDto request);
    Task<AuthenticationResultDto> DisableTwoFactorAsync(string userId, VerifyTwoFactorRequestDto request);
    Task<RecoveryCodesDto?> GenerateRecoveryCodesAsync(string userId);
    Task<AuthenticationResultDto> ExternalLoginAsync(string provider, string providerKey, string email, string fullName, string? ipAddress, string? userAgent);
}
