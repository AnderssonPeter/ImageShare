using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;

namespace ImageShare.Browsing;

internal sealed class GetEntriesQueryHandler(
    ImageEnumerator imageEnumerator,
    IUser user)
    : IQueryHandler<GetEntriesQuery, Results<Ok<PaginatedResult<FolderEntry>>, UnauthorizedHttpResult, BadRequest, NotFound>>
{
    private const int MaxPageSize = 500;

    public ValueTask<Results<Ok<PaginatedResult<FolderEntry>>, UnauthorizedHttpResult, BadRequest, NotFound>> Handle(
        GetEntriesQuery request,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated)
        {
            return new(TypedResults.Unauthorized());
        }

        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > MaxPageSize)
        {
            return new(TypedResults.BadRequest());
        }

        if (request.Path.HasRootFolder)
        {
            try
            {
                user.EnsureCanAccessFolder(request.Path);
            }
            catch (FolderAccessDeniedException)
            {
                return new(TypedResults.NotFound());
            }
        }

        var entries = CollectEntries(imageEnumerator, request.Path, user);

        var result = TypedResults.Ok(PaginatedResult<FolderEntry>.Paginate(entries, request.Page, request.PageSize));
        return new(result);
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

                entries.Add(new FolderEntry { Name = item.Name, Type = EntryType.Folder });
            }
            else if (relativePath.HasRootFolder && TryGetVisibleFileName(enumerator, item.Name, out var fileName) && seenFiles.Add(fileName))
            {
                entries.Add(new FolderEntry { Name = fileName, Type = EntryType.File });
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
