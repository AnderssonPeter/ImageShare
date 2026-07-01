using System.Diagnostics;

namespace ImageShare.Authentication;

public class User
{
    public required bool IsAuthenticated { get; init; }
    public required string Name { get; init; }
    public required string ImageShareFilter { get; init; }

    public User(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext ?? throw new UnreachableException("Failed to get http context");

        if (context.User.Identity?.IsAuthenticated != true)
        {
            this.IsAuthenticated = false;
            this.Name = "";
            this.ImageShareFilter = "";
            return;
        }

        var name =
            context.User.Claims.SingleOrDefault(c => c.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value ??
            context.User.Claims.SingleOrDefault(c => c.Type.Equals("display_name", StringComparison.OrdinalIgnoreCase))?.Value ??
            throw new InvalidOperationException("Failed to get username");

        var imageShareFilter = context.User.Claims.Single(c => c.Type.Equals("image_share_filter")).Value;

        this.IsAuthenticated = true;
        this.Name = name;
        this.ImageShareFilter = imageShareFilter;
    }
}
