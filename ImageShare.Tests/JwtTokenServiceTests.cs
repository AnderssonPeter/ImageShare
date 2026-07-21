using System.Security.Claims;
using ImageShare.Authentication;
using ImageShare.Errors;
using Microsoft.Extensions.Options;

namespace ImageShare.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(string? signingKey = null)
    {
        var settings = new JwtSettings
        {
            Issuer = "ImageShare",
            Audience = "ImageShare",
            SigningKey = signingKey ?? "test-signing-key-must-be-at-least-32-characters-long",
        };
        return new JwtTokenService(Options.Create(settings));
    }

    [Test]
    public async Task CreateToken_ReturnsValidToken()
    {
        // Arrange
        var service = CreateService();
        var name = "alice";
        var filter = "vacation/*";
        var expiration = DateTime.UtcNow.AddHours(1);

        // Act
        var token = service.CreateToken(name, filter, expiration);

        // Assert
        await Assert.That(token).IsNotNull();
        await Assert.That(token.Split('.').Length).IsEqualTo(3);
    }

    [Test]
    public async Task ValidateTokenAsync_ValidToken_ReturnsPrincipalWithFilterClaim()
    {
        // Arrange
        var service = CreateService();
        var name = "alice";
        var filter = "vacation/*";
        var expiration = DateTime.UtcNow.AddHours(1);
        var token = service.CreateToken(name, filter, expiration);

        // Act
        var principal = await service.ValidateTokenAsync(token);

        // Assert
        await Assert.That(principal).IsNotNull();
        var filterClaim = principal.Claims.Single(c => c.Type.Equals(ImageShareClaims.ImageShareFilter, StringComparison.OrdinalIgnoreCase));
        await Assert.That(filterClaim.Value).IsEqualTo(filter);
        var nameClaim = principal.Claims.Single(c => c.Type.Equals(ImageShareClaims.Name, StringComparison.OrdinalIgnoreCase));
        await Assert.That(nameClaim.Value).IsEqualTo(name);
    }

    [Test]
    public async Task ValidateTokenAsync_ExpiredToken_ThrowsBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var token = service.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(-1));

        // Act
        // Assert
        await Assert.That(async () => await service.ValidateTokenAsync(token)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_TamperedToken_ThrowsBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var token = service.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(1));
        var tamperedToken = token[..^5] + "AAAAA";

        // Act
        // Assert
        await Assert.That(async () => await service.ValidateTokenAsync(tamperedToken)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_WrongSigningKey_ThrowsBadRequestException()
    {
        // Arrange
        var createService = CreateService("first-signing-key-must-be-at-least-32-chars");
        var validateService = CreateService("second-signing-key-must-be-at-least-32-chars");
        var token = createService.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await validateService.ValidateTokenAsync(token)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_GarbageToken_ThrowsBadRequestException()
    {
        // Arrange
        var service = CreateService();

        // Act
        // Assert
        await Assert.That(async () => await service.ValidateTokenAsync("not-a-valid-jwt")).Throws<BadRequestException>();
    }
}
