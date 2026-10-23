using System.ComponentModel.DataAnnotations;
using TaskFlow.Service.Authentication.DTOs;

namespace TaskFlow.Tests;

public class AuthenticationDtoTests
{
    [Fact]
    public void RegisterRequest_RejectsMismatchedPasswords()
    {
        var request = new RegisterRequestDto
        {
            FullName = "TaskFlow User",
            Email = "user@example.com",
            Password = "Secure1!Password",
            ConfirmPassword = "Different1!Password"
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.ErrorMessage == "Passwords do not match.");
    }
}
