using System.Security.Claims;
using ImageShare.Errors;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageShare.Authentication;

public sealed class JwtTokenValidator(IOptions<JwtSettings> jwtSettings)
{
    private readonly JwtSettings _settings = jwtSettings.Value;

    private readonly SymmetricSecurityKey _symmetricSecurityKey = new(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Value.SigningKey));

    public async ValueTask<ClaimsPrincipal> ValidateTokenAsync(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = _symmetricSecurityKey,
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
