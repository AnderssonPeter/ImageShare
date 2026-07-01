namespace ImageShare.Authentication;

public interface IUser
{
    bool IsAuthenticated { get; }
    string Name { get; }
    bool CanAccessFolder(string folder);
}
