using ImageShare.Browsing;

namespace ImageShare.Authentication;

public interface IUser
{
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    string Name { get; }
    bool CanAccessFolder(string folder);
    void EnsureAuthenticated();
    void EnsureAdmin();
    void EnsureCanAccessFolder(RelativePath path);
}
