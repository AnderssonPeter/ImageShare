using System.Security.Claims;
using ImageShare.Authentication;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace ImageShare.Logging;

internal sealed class HttpContextEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    private const string AnonymousUser = "anonymous";
    private const string UnknownAddress = "unknown";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
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

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ClientIP", remoteAddress));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserName", userName));
    }
}
