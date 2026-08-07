using CompressedStaticFiles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.SpaServices.StaticFiles;

namespace ImageShare.Spa;

/// <summary>
/// Hosts the React single-page application built by the Vite project in <c>../frontend</c>.
/// Two states are supported:
/// <list type="bullet">
/// <item><b>Development</b> — requests that no backend endpoint matches are proxied to the
/// Vite dev server, so <c>/</c>, client-side routes, modules and HMR WebSockets are all served by Vite.</item>
/// <item><b>Production</b> — the compiled assets in <c>../frontend/dist</c> are served from disk,
/// and unmapped requests fall back to <c>index.html</c> so client-side routing works.</item>
/// </list>
/// Every request that reaches the SPA branch must be authenticated; unauthenticated requests are
/// challenged with the OpenID Connect handler so the browser is redirected to the identity provider
/// before any SPA content — HTML, script, asset, or proxied dev-server response — is served.
/// </summary>
public static class SpaExtensions
{
    /// <summary>Base URI of the Vite/React development server started by <c>pnpm dev</c>.</summary>
    public const string DevelopmentServerUri = "http://localhost:5000";

    public static void AddSpaHosting(this IServiceCollection services)
    {

        services.AddSpaStaticFiles(configuration => configuration.RootPath = "Client");
        services.AddCompressedStaticFiles();
    }

    /// <summary>
    /// Adds the SPA hosting middleware. Place it after authentication/authorization so the SPA
    /// is only reached for requests the backend did not handle. Requests with no matched endpoint
    /// (i.e. SPA routes and assets) are branched off; the branch first challenges any
    /// unauthenticated request via OpenID Connect, then either proxies to the Vite dev server
    /// (development) or serves the compiled assets from disk (production). Backend endpoints
    /// (including OpenAPI, Scalar and the OIDC callback) keep working because they have an endpoint.
    /// </summary>
    public static IApplicationBuilder UseSpaHosting(this IApplicationBuilder application, IWebHostEnvironment environment)
    {
        application.MapWhen(
            context => context.GetEndpoint() is null,
            branch =>
            {
                branch.Use(ChallengeIfUnauthenticated);

                if (environment.IsDevelopment())
                {
                    branch.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer(DevelopmentServerUri));
                }
                else
                {
                    var spaFileProvider = branch.ApplicationServices.GetRequiredService<ISpaStaticFileProvider>().FileProvider;
                    var spaFileOptions = new StaticFileOptions
                    {
                        FileProvider = spaFileProvider,
                    };

                    branch.UseCompressedStaticFiles(spaFileOptions);

                    // Fall back to index.html for client-side routes that map to no static file, so
                    // deep links and refreshes hand the SPA shell to the browser. The request is
                    // re-dispatched to /index.html through the compressed static files pipeline so the
                    // precompressed variants and content-type handling apply to the fallback too.
                    var indexPipeline = branch.New();
                    indexPipeline.UseCompressedStaticFiles(spaFileOptions);
                    var serveIndexHtml = indexPipeline.Build();

                    branch.Run(async context =>
                    {
                        context.Request.Path = "/index.html";
                        await serveIndexHtml(context);
                    });
                }
            });

        return application;
    }

    private static async Task ChallengeIfUnauthenticated(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
            return;
        }

        await next(context);
    }
}
