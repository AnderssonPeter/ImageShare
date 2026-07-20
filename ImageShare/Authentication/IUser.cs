using ImageShare.Browsing;

namespace ImageShare.Authentication;

public interface IUser
{
    bool IsAuthenticated { get; }
    string Name { get; }
    bool CanAccessFolder(string folder);
    void EnsureAuthenticated();
    void EnsureCanAccessFolder(RelativePath path);
}
