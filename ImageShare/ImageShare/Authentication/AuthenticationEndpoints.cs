using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace ImageShare.Authentication;

public static class AuthenticationEndpoints
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

        endpoints.MapGet("/user", (User user) =>
        {
            if (!user.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(user);
        }).RequireAuthorization();

        return endpoints;
    }
}
