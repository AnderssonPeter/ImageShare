using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Options;

namespace ImageShare.Authentication;

internal sealed class ApiKeyProvider(
    IOptions<ApiKeySettings> apiKeySettings,
    IOptions<OidcSettings> oidcSettings) : IApiKeyProvider
{

    public Task<IApiKey?> ProvideAsync(string key)
    {
        foreach (var (name, entry) in apiKeySettings.Value)
        {
            if (!string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                continue;
            }

            var claims = new List<Claim>
            {
                new(ImageShareClaims.Name, name),
                new(ImageShareClaims.ImageShareFilter, entry.Filter),
            };

            if (entry.IsAdmin)
            {
                claims.Add(new Claim(ImageShareClaims.Role, oidcSettings.Value.AdminRole));
            }

            return Task.FromResult<IApiKey?>(new ApiKey(entry.Key, name, claims));
        }

        return Task.FromResult<IApiKey?>(null);
    }
}
