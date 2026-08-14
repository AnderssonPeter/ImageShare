using ImageShare.Authentication;
using ImageShare.Errors;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Browsing;

internal sealed class GetEntriesQueryHandler(
    ImageEnumerator imageEnumerator,
    IUser user)
    : IQueryHandler<GetEntriesQuery, Ok<IReadOnlyList<FolderEntry>>>
{
    public ValueTask<Ok<IReadOnlyList<FolderEntry>>> Handle(
        GetEntriesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Path.HasRootFolder && !user.CanAccessFolder(request.Path.RootFolder))
        {
            throw new NotFoundException($"Folder '{request.Path}' was not found.");
        }

        if (request.Path.HasRootFolder && !imageEnumerator.GetDirectoryContents(request.Path).Exists)
        {
            throw new NotFoundException($"Folder '{request.Path}' was not found.");
        }

        var entries = CollectEntries(imageEnumerator, request.Path, user);

        return new(TypedResults.Ok<IReadOnlyList<FolderEntry>>(entries));
    }

    private static List<FolderEntry> CollectEntries(ImageEnumerator enumerator, RelativePath relativePath, IUser currentUser)
    {
        var entries = new List<FolderEntry>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in enumerator.GetDirectoryContents(relativePath))
        {
            if (item.IsDirectory)
            {
                if (!relativePath.HasRootFolder && !currentUser.CanAccessFolder(item.Name))
                {
                    continue;
                }

                var folderPath = relativePath.HasRootFolder ? relativePath.Combine(item.Name) : new RelativePath(item.Name);
                if (!enumerator.HasVisibleContent(folderPath))
                {
                    continue;
                }

                entries.Add(new FolderEntry { Name = item.Name, Path = folderPath.Value, Type = EntryType.Folder });
            }
            else if (relativePath.HasRootFolder && TryGetVisibleFileName(enumerator, item.Name, out var fileName) && seenFiles.Add(fileName))
            {
                entries.Add(new FolderEntry { Name = fileName, Path = relativePath.Combine(fileName).Value, Type = EntryType.File });
            }
        }

        entries.Sort(CompareEntries);

        return entries;
    }

    private static bool TryGetVisibleFileName(ImageEnumerator enumerator, string name, out string fileName)
    {
        var filePath = new RelativePath(name);
        fileName = "";
        if (filePath.IsThumbnail)
        {
            return false;
        }

        if (!enumerator.IsImageFile(name))
        {
            return false;
        }

        fileName = filePath.FileNameWithoutExtension;
        return true;
    }

    private static int CompareEntries(FolderEntry left, FolderEntry right)
    {
        if (left.Type != right.Type)
        {
            return left.Type == EntryType.Folder ? -1 : 1;
        }

        return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
    }
}
