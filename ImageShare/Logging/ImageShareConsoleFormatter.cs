using System.Globalization;
using ImageShare.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace ImageShare.Logging;

internal sealed class ImageShareConsoleFormatter(IHttpContextAccessor httpContextAccessor) : ConsoleFormatter(FormatterName)
{
    internal const string FormatterName = "ImageShare";

    private const string AnonymousUser = "anonymous";
    private const string UnknownAddress = "unknown";

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var level = GetLevelAbbreviation(logEntry.LogLevel);
        var (clientIp, userName) = GetContextValues();

        textWriter.Write('[');
        textWriter.Write(timestamp);
        textWriter.Write(' ');
        textWriter.Write(level);
        if (clientIp != null)
        {
            textWriter.Write("] [");
        }

        textWriter.Write(clientIp);
        if (userName != null)
        {
            textWriter.Write("] [");
        }

        textWriter.Write(userName);
        textWriter.Write("] ");
        textWriter.WriteLine(message);

        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine(logEntry.Exception);
        }
    }

    private static string GetLevelAbbreviation(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Information",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Critical",
        _ => "???",
    };

    private (string? ClientIp, string? UserName) GetContextValues()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return (null, null);
        }

        var remoteAddress = context.Connection.RemoteIpAddress?.ToString() ?? UnknownAddress;

        var identity = context.User.Identity;
        string userName;
        if (identity is { IsAuthenticated: true })
        {
            userName =
                context.User.Claims.SingleOrDefault(claim => claim.Type.Equals(ImageShareClaims.Name, StringComparison.OrdinalIgnoreCase))?.Value ??
                context.User.Claims.SingleOrDefault(claim => claim.Type.Equals(ImageShareClaims.DisplayName, StringComparison.OrdinalIgnoreCase))?.Value ??
                AnonymousUser;
        }
        else
        {
            userName = AnonymousUser;
        }

        return (remoteAddress, userName);
    }
}
