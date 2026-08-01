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
/// </summary>
public static class SpaExtensions
{
    /// <summary>Base URI of the Vite/React development server started by <c>pnpm dev</c>.</summary>
    public const string DevelopmentServerUri = "http://localhost:5000";

    public static void AddSpaHosting(this IServiceCollection services) =>
        services.AddSpaStaticFiles(configuration =>
            configuration.RootPath = "Client");

    /// <summary>
    /// Adds the SPA hosting middleware. Place it after authentication/authorization so the SPA
    /// is only reached for requests the backend did not handle. In development the proxy is a
    /// terminal branch gated on <see cref="HttpContextExtensions.GetEndpoint"/> being null, so
    /// backend endpoints (including OpenAPI and Scalar) keep working; everything else goes to Vite.
    /// In production the compiled assets are served from disk via <see cref="SpaStaticFilesExtensions.UseSpaStaticFiles"/>.
    /// </summary>
    public static IApplicationBuilder UseSpaHosting(this IApplicationBuilder application, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            application.MapWhen(
                context => context.GetEndpoint() is null,
                branch => branch.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer(DevelopmentServerUri)));
        }
        else
        {
            application.UseSpaStaticFiles();
        }

        return application;
    }
}
