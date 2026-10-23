using TaskFlow.Service.Authentication.Tokens;

namespace TaskFlow.Tests;

public class TokenServiceTests
{
    private readonly TokenService _tokenService = new();

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueCryptographicValues()
    {
        var first = _tokenService.GenerateRefreshToken();
        var second = _tokenService.GenerateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.True(first.Length >= 80);
        Assert.DoesNotContain("+", first);
        Assert.DoesNotContain("/", first);
    }

    [Fact]
    public void HashToken_IsDeterministicAndDoesNotExposeRawToken()
    {
        const string rawToken = "a-sensitive-refresh-token";

        var firstHash = _tokenService.HashToken(rawToken);
        var secondHash = _tokenService.HashToken(rawToken);

        Assert.Equal(firstHash, secondHash);
        Assert.NotEqual(rawToken, firstHash);
        Assert.Equal(64, firstHash.Length);
    }
}
