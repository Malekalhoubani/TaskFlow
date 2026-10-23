using TaskFlow.Service.Authentication.DTOs;

namespace TaskFlow.Service.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResultDto> RegisterAsync(
        RegisterRequestDto request);

    Task<AuthenticationResultDto> LoginAsync(
        LoginRequestDto request);

    Task<AuthenticationResultDto> ConfirmEmailAsync(
        ConfirmEmailRequestDto request);

    Task<AuthenticationResultDto> ResendEmailConfirmationAsync(
        ResendEmailConfirmationRequestDto request);

    Task<AuthenticationResultDto> ForgotPasswordAsync(
        ForgotPasswordRequestDto request);

    Task<AuthenticationResultDto> ResetPasswordAsync(
        ResetPasswordRequestDto request);

    Task<AuthenticationResultDto> ChangePasswordAsync(
        string userId,
        ChangePasswordRequestDto request);
}
