using System.Security.Claims;
using ImageShare.Errors;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageShare.Authentication;

public sealed class JwtTokenValidator(IOptions<JwtSettings> jwtSettings)
{
    private readonly SymmetricSecurityKey securityKey = new(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Value.SigningKey));

    public async ValueTask<ClaimsPrincipal> ValidateTokenAsync(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = securityKey,
            ValidIssuer = jwtSettings.Value.Issuer,
            ValidAudience = jwtSettings.Value.Audience,
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
