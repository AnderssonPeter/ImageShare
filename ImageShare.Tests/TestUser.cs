using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.Errors;

namespace ImageShare.Tests;

public sealed class TestUser : IUser
{
    public bool IsAuthenticated { get; set; } = true;
    public bool IsAdmin { get; set; }
    public string Name { get; set; } = "test";
    private readonly HashSet<string> allowedFolders = [];

    public TestUser Allow(string folder)
    {
        allowedFolders.Add(folder);
        return this;
    }

    public bool CanAccessFolder(string folder) => allowedFolders.Contains(folder);

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
}
