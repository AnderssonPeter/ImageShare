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
        var matchingEntry = apiKeySettings.Value.Keys.FirstOrDefault(entry =>
            string.Equals(entry.Key, key, StringComparison.Ordinal));

        if (matchingEntry is null)
        {
            return Task.FromResult<IApiKey?>(null);
        }

        var claims = new List<Claim>
        {
            new(ImageShareClaims.Name, matchingEntry.Name),
            new(ImageShareClaims.ImageShareFilter, matchingEntry.Filter),
        };

        if (matchingEntry.IsAdmin)
        {
            claims.Add(new Claim(ImageShareClaims.Role, oidcSettings.Value.AdminRole));
        }

        return Task.FromResult<IApiKey?>(new ApiKey(matchingEntry.Key, matchingEntry.Name, claims));
    }
}
