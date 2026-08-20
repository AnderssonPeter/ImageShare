using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Console;

namespace ImageShare.Logging;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddImageShareConsoleFormatter(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, ImageShareConsoleFormatter>());
        builder.Services.Configure<ConsoleLoggerOptions>(options => options.FormatterName = ImageShareConsoleFormatter.FormatterName);
        return builder;
    }
}
