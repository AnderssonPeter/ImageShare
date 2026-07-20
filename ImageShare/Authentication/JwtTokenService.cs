using System.Security.Claims;
using ImageShare.Errors;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageShare.Authentication;

public class JwtTokenService(IOptions<JwtSettings> jwtSettings)
{
    private readonly JwtSettings _settings = jwtSettings.Value;
    private readonly SigningCredentials _signingCredentials = new(
        new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Value.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    public string CreateToken(string imageShareFilter, DateTime expiration)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expiration,
            SigningCredentials = _signingCredentials,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { ImageShareClaims.ImageShareFilter, imageShareFilter },
            },
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }

    public async ValueTask<ClaimsPrincipal> ValidateTokenAsync(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.SigningKey)),
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        };

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token, validationParameters);

        if (!result.IsValid)
        {
            throw new BadRequestException("The provided JWT token is invalid or has expired.");
        }

        return new ClaimsPrincipal(result.ClaimsIdentity);
    }
}
