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

        return TypedResults.Redirect(IsSafeReturnUrl(request.ReturnUrl) ? request.ReturnUrl : "/");
    }

    private static bool IsSafeReturnUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        // Reject protocol-relative and backslash-prefixed URLs so no protocol or host can sneak in.
        if (returnUrl.StartsWith("//", StringComparison.Ordinal) ||
            returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        if (returnUrl.Contains("..", StringComparison.Ordinal) ||
            returnUrl.Contains('\\'))
        {
            return false;
        }

        for (var index = 0; index < returnUrl.Length; index++)
        {
            if (char.IsControl(returnUrl, index))
            {
                return false;
            }
        }

        return true;
    }
}
