using ImageShare.Authentication;
using Microsoft.Extensions.Options;

namespace ImageShare.Tests;

public class JwtTokenIssuerTests
{
    private static JwtTokenIssuer CreateIssuer(string? signingKey = null)
    {
        var settings = new JwtSettings
        {
            Issuer = "ImageShare",
            Audience = "ImageShare",
            SigningKey = signingKey ?? "test-signing-key-must-be-at-least-32-characters-long",
        };
        return new JwtTokenIssuer(Options.Create(settings));
    }

    [Test]
    public async Task CreateToken_ReturnsValidToken()
    {
        // Arrange
        var issuer = CreateIssuer();
        var name = "alice";
        var filter = "vacation/*";
        var expiration = DateTime.UtcNow.AddHours(1);

        // Act
        var token = issuer.CreateToken(name, filter, expiration);

        // Assert
        await Assert.That(token).IsNotNull();
        await Assert.That(token.Split('.').Length).IsEqualTo(3);
    }
}
