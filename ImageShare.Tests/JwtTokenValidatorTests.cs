using System.Security.Claims;
using System.Text.Json.Nodes;
using ImageShare.Authentication;
using ImageShare.Errors;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageShare.Tests;

[MicrosoftDI]
public class JwtTokenValidatorTests(JwtTokenIssuer issuer, JwtTokenValidator validator)
{
    private const string RealSigningKey = "test-signing-key-must-be-at-least-32-characters-long";
    private const string RealIssuer = "ImageShare";
    private const string RealAudience = "ImageShare";

    private static JwtTokenIssuer CreateForger(string? signingKey = null, string? issuerName = null, string? audienceName = null)
    {
        var settings = new JwtSettings
        {
            Issuer = issuerName ?? RealIssuer,
            Audience = audienceName ?? RealAudience,
            SigningKey = signingKey ?? RealSigningKey,
        };
        return new JwtTokenIssuer(Options.Create(settings));
    }

    private static JwtTokenValidator CreateValidatorWithKey(string signingKey) =>
        new(Options.Create(new JwtSettings
        {
            Issuer = RealIssuer,
            Audience = RealAudience,
            SigningKey = signingKey,
        }));

    private static string CreateUnsignedToken(string name, string imageShareFilter, DateTime expiration)
    {
        var header = new Dictionary<string, object>
        {
            ["alg"] = "none",
            ["typ"] = "JWT",
        };
        var payload = new Dictionary<string, object>
        {
            ["iss"] = RealIssuer,
            ["aud"] = RealAudience,
            ["exp"] = new DateTimeOffset(expiration, TimeSpan.Zero).ToUnixTimeSeconds(),
            [ImageShareClaims.Name] = name,
            [ImageShareClaims.ImageShareFilter] = imageShareFilter,
        };

        var headerSegment = Base64UrlEncoder.Encode(System.Text.Json.JsonSerializer.Serialize(header));
        var payloadSegment = Base64UrlEncoder.Encode(System.Text.Json.JsonSerializer.Serialize(payload));
        return $"{headerSegment}.{payloadSegment}.";
    }

    private static string CreateTokenWithAlgorithm(string algorithm, DateTime? notBefore = null)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(RealSigningKey);
        if (algorithm == SecurityAlgorithms.HmacSha512 && keyBytes.Length < 64)
        {
            keyBytes = new byte[64];
            System.Text.Encoding.UTF8.GetBytes(RealSigningKey).CopyTo(keyBytes, 0);
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(key, algorithm);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = RealIssuer,
            Audience = RealAudience,
            NotBefore = notBefore,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [ImageShareClaims.Name] = "attacker",
                [ImageShareClaims.ImageShareFilter] = "admin/*",
            },
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string TamperClaim(string token, string claim, string newValue)
    {
        var segments = token.Split('.');
        var payload = JsonNode.Parse(Base64UrlEncoder.Decode(segments[1]))!;
        payload[claim] = newValue;
        return $"{segments[0]}.{Base64UrlEncoder.Encode(payload.ToJsonString())}.{segments[2]}";
    }

    [Test]
    public async Task ValidateTokenAsync_ValidToken_ReturnsPrincipalWithFilterClaim()
    {
        // Arrange
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
        var token = issuer.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(-1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(token)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_TamperedSignature_ThrowsBadRequestException()
    {
        // Arrange
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
        var forger = CreateForger(signingKey: "first-signing-key-must-be-at-least-32-chars");
        var mismatchedValidator = CreateValidatorWithKey("second-signing-key-must-be-at-least-32-chars");
        var token = forger.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await mismatchedValidator.ValidateTokenAsync(token)).Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_GarbageToken_ThrowsBadRequestException()
    {
        // Arrange
        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync("not-a-valid-jwt"))
            .Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_ForgedWithNoneAlgorithm_ThrowsBadRequestException()
    {
        // Arrange
        var forgedToken = CreateUnsignedToken("attacker", "admin/*", DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }

    [Test]
    [Arguments("wrong-signing-key-that-an-attacker-might-guess1")]
    [Arguments("another-guessed-key-at-least-32-characters-long")]
    [Arguments("00000000000000000000000000000000")]
    [Arguments("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public async Task ValidateTokenAsync_ForgedWithGuessedSigningKey_ThrowsBadRequestException(string guessedKey)
    {
        // Arrange
        var forger = CreateForger(signingKey: guessedKey);
        var forgedToken = forger.CreateToken("attacker", "admin/*", DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_ForgedWithTamperedFilterClaim_ThrowsBadRequestException()
    {
        // Arrange
        var legitimateToken = issuer.CreateToken("alice", "public/*", DateTime.UtcNow.AddHours(1));
        var forgedToken = TamperClaim(legitimateToken, ImageShareClaims.ImageShareFilter, "admin/*");

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_ForgedWithTamperedNameClaim_ThrowsBadRequestException()
    {
        // Arrange
        var legitimateToken = issuer.CreateToken("alice", "vacation/*", DateTime.UtcNow.AddHours(1));
        var forgedToken = TamperClaim(legitimateToken, ImageShareClaims.Name, "admin");

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_ForgedWithWrongIssuer_ThrowsBadRequestException()
    {
        // Arrange
        var forger = CreateForger(issuerName: "https://evil.example.com");
        var forgedToken = forger.CreateToken("attacker", "admin/*", DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_ForgedWithWrongAudience_ThrowsBadRequestException()
    {
        // Arrange
        var forger = CreateForger(audienceName: "https://evil.example.com");
        var forgedToken = forger.CreateToken("attacker", "admin/*", DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_ForgedWithFutureNotBefore_ThrowsBadRequestException()
    {
        // Arrange
        var forgedToken = CreateTokenWithAlgorithm(
            SecurityAlgorithms.HmacSha256,
            notBefore: DateTime.UtcNow.AddHours(1));

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }

    [Test]
    public async Task ValidateTokenAsync_ForgedWithAlternateAlgorithm_ThrowsBadRequestException()
    {
        // Arrange
        var forgedToken = CreateTokenWithAlgorithm(SecurityAlgorithms.HmacSha512);

        // Act
        // Assert
        await Assert.That(async () => await validator.ValidateTokenAsync(forgedToken))
            .Throws<BadRequestException>();
    }
}
