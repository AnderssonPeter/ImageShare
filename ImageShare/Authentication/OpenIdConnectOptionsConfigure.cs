using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace ImageShare.Authentication;

internal sealed class OpenIdConnectOptionsConfigure(IOptions<OidcSettings> oidcSettings) : IConfigureNamedOptions<OpenIdConnectOptions>
{
    private readonly OidcSettings _settings = oidcSettings.Value;

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (!string.Equals(name, OpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        options.Authority = _settings.Authority;
        options.ClientId = _settings.ClientId;
        options.ClientSecret = _settings.ClientSecret;
        options.ResponseType = _settings.ResponseType;
    }

    public void Configure(OpenIdConnectOptions options) => Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
}
