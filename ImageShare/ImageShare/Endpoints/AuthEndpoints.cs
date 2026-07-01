using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace ImageShare.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", (string? returnUrl) =>
        {
            var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/weatherforecast" : returnUrl;
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                new[] { OpenIdConnectDefaults.AuthenticationScheme });
        });

        endpoints.MapGet("/logout", () =>
            Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                new[] { CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme }));

        endpoints.MapGet("/user", (HttpContext context) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var claims = context.User.Claims.Select(c => new { c.Type, c.Value });
            return Results.Ok(new
            {
                context.User.Identity.Name,
                Claims = claims
            });
        }).RequireAuthorization();

        return endpoints;
    }
}
