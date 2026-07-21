using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageShare.Authentication;

public sealed class JwtTokenIssuer(IOptions<JwtSettings> jwtSettings)
{
    private readonly SigningCredentials signingCredentials = new(
        new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Value.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    public string CreateToken(string name, string imageShareFilter, DateTime expiration)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtSettings.Value.Issuer,
            Audience = jwtSettings.Value.Audience,
            Expires = expiration,
            SigningCredentials = signingCredentials,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                { ImageShareClaims.Name, name },
                { ImageShareClaims.ImageShareFilter, imageShareFilter },
            },
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }
}
