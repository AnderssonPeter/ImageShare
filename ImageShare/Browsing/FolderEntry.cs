namespace ImageShare.Browsing;

public sealed class FolderEntry
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required EntryType Type { get; init; }
}
