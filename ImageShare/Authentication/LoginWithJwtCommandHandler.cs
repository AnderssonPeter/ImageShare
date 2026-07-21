using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Authentication;

internal sealed class LoginWithJwtCommandHandler(
    JwtTokenValidator tokenValidator,
    IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<LoginWithJwtCommand, RedirectHttpResult>
{
    public async ValueTask<RedirectHttpResult> Handle(LoginWithJwtCommand request, CancellationToken cancellationToken)
    {
        var principal = await tokenValidator.ValidateTokenAsync(request.Token);
        var filterClaim = principal.Claims
            .Single(claim => claim.Type.Equals(ImageShareClaims.ImageShareFilter, StringComparison.OrdinalIgnoreCase));
        var nameClaim = principal.Claims
            .Single(claim => claim.Type.Equals(ImageShareClaims.Name, StringComparison.OrdinalIgnoreCase));

        var identity = new ClaimsIdentity(
            new[] { nameClaim, filterClaim },
            CookieAuthenticationDefaults.AuthenticationScheme);

        var context = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("Failed to get http context");
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return TypedResults.Redirect("/");
    }
}
