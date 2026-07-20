using System.Diagnostics;
using System.Security.Claims;
using ImageShare.Browsing;
using ImageShare.Errors;
using Microsoft.Extensions.Options;

namespace ImageShare.Authentication;

public class User : IUser
{
    private readonly ImageShareFilterService _imageShareFilterService;

    public required bool IsAuthenticated { get; init; }
    public required bool IsAdmin { get; init; }
    public required string Name { get; init; }
    private string ImageShareFilter { get; init; }

    public bool CanAccessFolder(string folder) =>
        _imageShareFilterService.GetImageShareFilterRegex(ImageShareFilter).IsMatch(folder);

    public void EnsureAuthenticated()
    {
        if (!IsAuthenticated)
        {
            throw new NotAuthenticatedException();
        }
    }

    public void EnsureAdmin()
    {
        if (!IsAdmin)
        {
            throw new ForbiddenException("Administrator access is required for this operation.");
        }
    }

    public void EnsureCanAccessFolder(RelativePath path)
    {
        if (!CanAccessFolder(path.RootFolder))
        {
            throw new FolderAccessDeniedException(path);
        }
    }

    public User(
        IHttpContextAccessor httpContextAccessor,
        ImageShareFilterService imageShareFilterService,
        IOptions<OidcSettings> oidcSettings)
    {
        _imageShareFilterService = imageShareFilterService;
        var adminRole = oidcSettings.Value.AdminRole;

        var context = httpContextAccessor.HttpContext ?? throw new UnreachableException("Failed to get http context");

        if (context.User.Identity?.IsAuthenticated != true)
        {
            IsAuthenticated = false;
            IsAdmin = false;
            Name = "";
            ImageShareFilter = "";
            return;
        }

        var name =
            context.User.Claims.SingleOrDefault(c => c.Type.Equals(ImageShareClaims.Name, StringComparison.OrdinalIgnoreCase))?.Value ??
            context.User.Claims.SingleOrDefault(c => c.Type.Equals(ImageShareClaims.DisplayName, StringComparison.OrdinalIgnoreCase))?.Value ??
            throw new InvalidOperationException("Failed to get username");

        var imageShareFilter = context.User.Claims.Single(c => c.Type.Equals(ImageShareClaims.ImageShareFilter, StringComparison.OrdinalIgnoreCase)).Value;

        var isAdmin = context.User.Claims
            .Where(c => c.Type.Equals(ImageShareClaims.Role, StringComparison.OrdinalIgnoreCase) ||
                        c.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
            .Any(c => c.Value.Equals(adminRole, StringComparison.OrdinalIgnoreCase));

        IsAuthenticated = true;
        IsAdmin = isAdmin;
        Name = name;
        ImageShareFilter = imageShareFilter;
    }
}
