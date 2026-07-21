using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace ImageShare.Authentication;

internal sealed class OpenIdConnectOptionsConfigure(IOptions<OidcSettings> oidcSettings) : IConfigureNamedOptions<OpenIdConnectOptions>
{

    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (!string.Equals(name, OpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        options.Authority = oidcSettings.Value.Authority;
        options.ClientId = oidcSettings.Value.ClientId;
        options.ClientSecret = oidcSettings.Value.ClientSecret;
        options.ResponseType = oidcSettings.Value.ResponseType;
    }

    public void Configure(OpenIdConnectOptions options) => Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
}
