using ImageShare.Authentication;
using ImageShare.Browsing;

namespace ImageShare.Tests;

public sealed class TestUser : IUser
{
    public bool IsAuthenticated { get; set; } = true;
    public string Name { get; set; } = "test";
    private readonly HashSet<string> _allowedFolders = [];

    public TestUser Allow(string folder)
    {
        _allowedFolders.Add(folder);
        return this;
    }

    public bool CanAccessFolder(string folder) => _allowedFolders.Contains(folder);
    public void EnsureCanAccessFolder(RelativePath path)
    {
        if (!CanAccessFolder(path.RootFolder))
        {
            throw new FolderAccessDeniedException(path);
        }
    }
}
