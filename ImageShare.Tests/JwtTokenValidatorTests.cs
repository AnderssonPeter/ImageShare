using System.Security.Claims;
using ImageShare.Authentication;
using ImageShare.Errors;
using Microsoft.Extensions.Options;

namespace ImageShare.Tests;

public class JwtTokenValidatorTests
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

    private static JwtTokenValidator CreateValidator(string? signingKey = null)
    {
        var settings = new JwtSettings
        {
            Issuer = "ImageShare",
            Audience = "ImageShare",
            SigningKey = signingKey ?? "test-signing-key-must-be-at-least-32-characters-long",
        };
        return new JwtTokenValidator(Options.Create(settings));
    }

    [Test]
    public async Task ValidateTokenAsync_ValidToken_ReturnsPrincipalWithFilterClaim()
    {
        // Arrange
        var issuer = CreateIssuer();
        var validator = CreateValidator();
        var name = "alice";
        var filter = "vacation/*";
        var expiration = DateTime.UtcNow.AddHours(1);
        var token = issuer.CreateToken(name, filter, expiration);

        // Act
        var principal = await validator.ValidateTokenAsync(token);

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
        var issuer = CreateIssuer();
        var validator = CreateValidator();
        var token = issuer.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(-1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(token)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_TamperedToken_ThrowsBadRequestException()
    {
        // Arrange
        var issuer = CreateIssuer();
        var validator = CreateValidator();
        var token = issuer.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(1));
        var tamperedToken = token[..^5] + "AAAAA";

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(tamperedToken)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_WrongSigningKey_ThrowsBadRequestException()
    {
        // Arrange
        var issuer = CreateIssuer("first-signing-key-must-be-at-least-32-chars");
        var validator = CreateValidator("second-signing-key-must-be-at-least-32-chars");
        var token = issuer.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(token)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_GarbageToken_ThrowsBadRequestException()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync("not-a-valid-jwt")).Throws<BadRequestException>();
    }
}
