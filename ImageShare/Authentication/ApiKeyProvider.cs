using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Options;

namespace ImageShare.Authentication;

internal sealed class ApiKeyProvider(
    IOptions<ApiKeySettings> apiKeySettings,
    IOptions<OidcSettings> oidcSettings) : IApiKeyProvider
{
    private readonly ApiKeySettings _settings = apiKeySettings.Value;
    private readonly OidcSettings _oidcSettings = oidcSettings.Value;

    public Task<IApiKey?> ProvideAsync(string key)
    {
        var matchingEntry = _settings.Keys.FirstOrDefault(entry =>
            string.Equals(entry.Key, key, StringComparison.Ordinal));

        if (matchingEntry is null)
        {
            return Task.FromResult<IApiKey?>(null);
        }

        var claims = new List<Claim>
        {
            new("name", matchingEntry.Name),
            new("image_share_filter", matchingEntry.Filter),
        };

        if (matchingEntry.IsAdmin)
        {
            claims.Add(new Claim("role", _oidcSettings.AdminRole));
        }

        return Task.FromResult<IApiKey?>(new ApiKey(matchingEntry.Key, matchingEntry.Name, claims));
    }
}
