using System.Security.Cryptography;
using System.Text;
using ImageShare.Errors;
using Microsoft.Extensions.Options;

namespace ImageShare.UsageAgreement;

public sealed class UsageAgreementConsent(IOptions<UsageAgreementOptions> options, IHttpContextAccessor httpContextAccessor) : IUsageAgreement
{
    public const string CookieName = "usage-agreement";

    private static readonly CookieOptions acceptCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromDays(365),
        IsEssential = true,
    };

    public bool IsEnabled => options.Value.IsEnabled;

    public bool IsAccepted
    {
        get
        {
            if (!IsEnabled)
            {
                return true;
            }

            var context = httpContextAccessor.HttpContext;
            if (context is null)
            {
                return false;
            }

            if (!context.Request.Cookies.TryGetValue(CookieName, out var cookieHash) || string.IsNullOrEmpty(cookieHash))
            {
                return false;
            }

            var match = options.Value.FindBestMatch(context.Request.Headers.Accept.ToString());
            return match is not null && string.Equals(ComputeHash(match.Text), cookieHash, StringComparison.Ordinal);
        }
    }

    public void EnsureAccepted()
    {
        if (!IsAccepted)
        {
            throw new UsageAgreementNotAcceptedException();
        }
    }

    public void Accept()
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Failed to get http context");

        var match = options.Value.FindBestMatch(context.Request.Headers.Accept.ToString());
        if (match is null)
        {
            throw new NotFoundException("No usage agreement is configured.");
        }

        context.Response.Cookies.Append(CookieName, ComputeHash(match.Text), acceptCookieOptions);
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
