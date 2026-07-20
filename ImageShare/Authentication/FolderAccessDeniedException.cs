using ImageShare.Browsing;

namespace ImageShare.Authentication;

public sealed class FolderAccessDeniedException : Exception
{
    public RelativePath Path { get; }

    public FolderAccessDeniedException(RelativePath path) : base($"Access denied to folder '{path}'") => Path = path;

    public FolderAccessDeniedException(RelativePath path, Exception innerException) : base($"Access denied to folder '{path}'", innerException) => Path = path;

    public FolderAccessDeniedException() : base("Access denied to folder") => Path = default;

    public FolderAccessDeniedException(string message) : base(message) => Path = default;

    public FolderAccessDeniedException(string message, Exception innerException) : base(message, innerException) => Path = default;
}
