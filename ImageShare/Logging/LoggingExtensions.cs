using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ImageShare.Logging;

public static class LoggingExtensions
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Options types are annotated with DynamicallyAccessedMembers.")]
    public static IServiceCollection AddReverseProxy(this IServiceCollection services)
    {
        services.AddOptions<ReverseProxyOptions>()
            .BindConfiguration("ReverseProxy")
            .Validated();

        return services;
    }

    public static ILoggingBuilder AddImageShareConsoleFormatter(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, ImageShareConsoleFormatter>());
        builder.Services.Configure<ConsoleLoggerOptions>(options => options.FormatterName = ImageShareConsoleFormatter.FormatterName);
        return builder;
    }

    public static IApplicationBuilder UseReverseProxy(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<ReverseProxyOptions>>().Value;

        if (options.Enabled)
        {
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            };

            foreach (var proxy in options.KnownProxies)
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    forwardedHeadersOptions.KnownProxies.Add(address);
                }
            }

            app.UseForwardedHeaders(forwardedHeadersOptions);
        }

        return app;
    }
}
