using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("authentication").WithTags("authentication");

        group.MapGet("/login", (string? returnUrl) =>
        {
            var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            return TypedResults.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                new[] { OpenIdConnectDefaults.AuthenticationScheme });
        });

        group.MapGet("/logout", () =>
            TypedResults.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                new[] { CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme }));

        group.MapGet("/user", Ok<IUser> (IUser user) =>
        {
            user.EnsureAuthenticated();
            return TypedResults.Ok(user);
        }).RequireAuthorization().ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("authentication").WithTags("authentication");
        group.MapGet("/token/generate", async (IMediator mediator, [AsParameters] GenerateTokenQuery request) =>
            await mediator.Send(request))
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/login/jwt/{token}", async (IMediator mediator, [AsParameters] LoginWithJwtCommand request) =>
            await mediator.Send(request))
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
