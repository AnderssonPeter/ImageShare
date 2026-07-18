using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", (string? returnUrl) =>
        {
            var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            return TypedResults.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                new[] { OpenIdConnectDefaults.AuthenticationScheme });
        });

        endpoints.MapGet("/logout", () =>
            TypedResults.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                new[] { CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme }));

        endpoints.MapGet("/user", Results<Ok<IUser>, UnauthorizedHttpResult> (IUser user) =>
        {
            if (!user.IsAuthenticated)
            {
                return TypedResults.Unauthorized();
            }

            return TypedResults.Ok(user);
        }).RequireAuthorization();

        return endpoints;
    }
}
