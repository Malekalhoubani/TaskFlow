using TaskFlow.Service.Authentication.Models;

namespace TaskFlow.Service.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(
        RegisterRequest request);

    Task<AuthenticationResult> LoginAsync(
        LoginRequest request);

    Task<AuthenticationResult> ConfirmEmailAsync(
        ConfirmEmailRequest request);

    Task<AuthenticationResult> ResendEmailConfirmationAsync(
        ResendEmailConfirmationRequest request);
}
