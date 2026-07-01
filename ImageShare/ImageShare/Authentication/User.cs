using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ImageShare.Authentication;

public class User
{
    public required bool IsAuthenticated { get; init; }
    public required string Name { get; init; }
    public required string ImageShareFilter { get; init; }

    public bool CanAccessFolder(string folder)
    {
        if (string.IsNullOrEmpty(ImageShareFilter))
            return false;

        var patterns = ImageShareFilter.Split('|');

        var regexParts = new List<string>();

        foreach (var pattern in patterns)
        {
            var escaped = Regex.Escape(pattern);
            escaped = escaped.Replace("\\*", "[^/]*");
            escaped = escaped.Replace("\\?", "[^/]");
            regexParts.Add("^" + escaped + "$");
        }

        var regex = new Regex(string.Join('|', regexParts), RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(0.25));
        return regex.IsMatch(folder);
    }

    public User(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext ?? throw new UnreachableException("Failed to get http context");

        if (context.User.Identity?.IsAuthenticated != true)
        {
            IsAuthenticated = false;
            Name = "";
            ImageShareFilter = "";
            return;
        }

        var name =
            context.User.Claims.SingleOrDefault(c => c.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value ??
            context.User.Claims.SingleOrDefault(c => c.Type.Equals("display_name", StringComparison.OrdinalIgnoreCase))?.Value ??
            throw new InvalidOperationException("Failed to get username");

        var imageShareFilter = context.User.Claims.Single(c => c.Type.Equals("image_share_filter")).Value;

        IsAuthenticated = true;
        Name = name;
        ImageShareFilter = imageShareFilter;
    }
}
