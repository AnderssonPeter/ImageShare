using ImageShare.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Mirality.FileProviders;
using Mirality.FileProviders.InMemory;

namespace ImageShare.Tests;

public sealed class ImageShareWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestApiKey = "test-api-key";

    public InMemoryFileProvider FileProvider { get; } = new();

    public HttpClient CreateClientWithApiKey()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(AuthenticationExtensions.ApiKeyHeaderName, TestApiKey);
        return client;
    }

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
                ["ApiKeys:Keys:0:Key"] = TestApiKey,
                ["ApiKeys:Keys:0:Name"] = "Test",
                ["ApiKeys:Keys:0:Filter"] = "*",
                ["ApiKeys:Keys:0:IsAdmin"] = "true",
                ["Jwt:Issuer"] = "ImageShare",
                ["Jwt:Audience"] = "ImageShare",
                ["Jwt:SigningKey"] = "test-signing-key-must-be-at-least-32-characters-long",
                ["Storage:BasePath"] = "images",
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
                });

            services.RemoveAll<IFileProvider>();
            services.RemoveAll<IWritableFileProvider>();
            services.RemoveAll<ISyncWritableFileProvider>();
            services.AddSingleton<InMemoryFileProvider>(FileProvider);
            services.AddSingleton<ISyncWritableFileProvider>(sp => sp.GetRequiredService<InMemoryFileProvider>());
            services.AddSingleton<IWritableFileProvider>(sp => sp.GetRequiredService<InMemoryFileProvider>());
            services.AddSingleton<IFileProvider>(sp => sp.GetRequiredService<InMemoryFileProvider>());
        });
    }
}
