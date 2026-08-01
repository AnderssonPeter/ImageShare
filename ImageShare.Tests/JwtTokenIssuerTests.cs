using ImageShare.Authentication;

namespace ImageShare.Tests;

[MicrosoftDI]
public class JwtTokenIssuerTests(JwtTokenIssuer issuer)
{
    [Test]
    public async Task CreateToken_ReturnsValidToken()
    {
        // Arrange
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
