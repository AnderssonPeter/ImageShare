using ImageShare.Browsing;

namespace ImageShare.Errors;

public sealed class FolderAccessDeniedException : ImageShareException
{
    public RelativePath Path { get; }

    public FolderAccessDeniedException() : base("Access denied to folder.") => Path = default;

    public FolderAccessDeniedException(RelativePath path) : base($"Access denied to folder '{path}'.") => Path = path;

    public FolderAccessDeniedException(string message) : base(message) => Path = default;

    public FolderAccessDeniedException(string message, Exception innerException) : base(message, innerException) => Path = default;
}
