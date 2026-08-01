using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace ImageShare.Authentication;

public static class RateLimitExtensions
{
    public const string UnauthenticatedPolicy = "unauthenticated";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration("RateLimit")
            .Validated();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(UnauthenticatedPolicy, httpContext =>
            {
                var settings = httpContext.RequestServices
                    .GetRequiredService<IOptions<RateLimitSettings>>().Value;

                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{partitionKey}:{UnauthenticatedPolicy}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = settings.PermitLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app) =>
        app.UseRateLimiter();
}
