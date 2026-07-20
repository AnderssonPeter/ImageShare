using System.Diagnostics;
using ImageShare.Browsing;
using ImageShare.Errors;

namespace ImageShare.Authentication;

public class User : IUser
{
    private readonly ImageShareFilterService _imageShareFilterService;

    public required bool IsAuthenticated { get; init; }
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

    public void EnsureCanAccessFolder(RelativePath path)
    {
        if (!CanAccessFolder(path.RootFolder))
        {
            throw new FolderAccessDeniedException(path);
        }
    }

    public User(IHttpContextAccessor httpContextAccessor, ImageShareFilterService imageShareFilterService)
    {
        _imageShareFilterService = imageShareFilterService;

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
