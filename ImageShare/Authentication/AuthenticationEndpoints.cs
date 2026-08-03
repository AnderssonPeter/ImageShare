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

        group.MapGet("/login", Login)
            .RequireRateLimiting(RateLimitExtensions.UnauthenticatedPolicy);

        group.MapGet("/logout", Logout)
            .RequireRateLimiting(RateLimitExtensions.UnauthenticatedPolicy);

        group.MapGet("/user", GetCurrentUser)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static ChallengeHttpResult Login(string? returnUrl)
    {
        var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
        return TypedResults.Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            new[] { OpenIdConnectDefaults.AuthenticationScheme });
    }

    private static SignOutHttpResult Logout() =>
        TypedResults.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            new[] { CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme });

    private static Ok<IUser> GetCurrentUser(IUser user)
    {
        user.EnsureAuthenticated();
        return TypedResults.Ok(user);
    }

    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("authentication").WithTags("authentication");
        group.MapGet("/token/generate", GenerateTokenAsync)
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/login/jwt/{token}", LoginWithJwtAsync)
            .RequireRateLimiting(RateLimitExtensions.UnauthenticatedPolicy)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<Ok<string>> GenerateTokenAsync(IMediator mediator, [AsParameters] GenerateTokenQuery request) =>
        await mediator.Send(request);

    private static async Task<RedirectHttpResult> LoginWithJwtAsync(IMediator mediator, [AsParameters] LoginWithJwtCommand request) =>
        await mediator.Send(request);
}
