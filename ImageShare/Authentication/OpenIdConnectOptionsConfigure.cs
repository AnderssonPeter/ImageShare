using System.Diagnostics;
using System.Security.Claims;
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

        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnTokenValidated = context =>
        {
            var principal = context.Principal ?? throw new UnreachableException("Failed to get principal during token validation");
            principal.AddIdentity(new ClaimsIdentity(new[]
            {
                new Claim(ImageShareClaims.AuthenticationSource, AuthenticationSource.OpenIdConnect.ToString()),
            }));
            return Task.CompletedTask;
        };
    }

    public void Configure(OpenIdConnectOptions options) => Configure(OpenIdConnectDefaults.AuthenticationScheme, options);
}
