using ImageShare.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.AspNetCore;

namespace ImageShare.Tests;

public sealed class ImageShareWebApplicationFactory : TestWebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenIdConnect:Authority"] = "https://test-authority",
                ["OpenIdConnect:ClientId"] = "test-client-id",
                ["OpenIdConnect:ClientSecret"] = "test-client-secret",
                ["OpenIdConnect:AdminRole"] = "admin",
                ["ApiKeys:Keys:Test:Key"] = TestApiKey,
                ["ApiKeys:Keys:Test:Filter"] = "*",
                ["ApiKeys:Keys:Test:IsAdmin"] = "true",
                ["Jwt:Issuer"] = "ImageShare",
                ["Jwt:Audience"] = "ImageShare",
                ["Jwt:SigningKey"] = "test-signing-key-must-be-at-least-32-characters-long",
                ["Storage:BasePath"] = "images",
                ["RateLimit:PermitLimit"] = "3",
                ["RateLimit:WindowSeconds"] = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options => options.HttpsPort = null);

            services.Configure<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>(
                Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = "https://test-authority";
                    options.ClientId = "test-client-id";
                    options.ClientSecret = "test-client-secret";
                    // A static configuration avoids the handler trying to fetch discovery metadata
                    // from the fake authority during a challenge, so challenges produce a clean 302.
                    options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                    {
                        AuthorizationEndpoint = "https://test-authority/authorize",
                        EndSessionEndpoint = "https://test-authority/logout",
                    };
                });

            services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
        });
    }
}
