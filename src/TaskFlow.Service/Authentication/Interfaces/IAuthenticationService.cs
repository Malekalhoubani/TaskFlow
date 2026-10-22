using TaskFlow.Service.Authentication.Models;

namespace TaskFlow.Service.Authentication.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request);

    Task<AuthenticationResult> LoginAsync(LoginRequest request);
}
