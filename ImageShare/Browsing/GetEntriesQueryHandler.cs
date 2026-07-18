using ImageShare.Authentication;
using ImageShare.ImageConversion;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ImageShare.Browsing;

internal sealed class GetEntriesQueryHandler(
    IFileProvider fileProvider,
    IOptions<ImageFormatOptions> imageFormats,
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

        PathHelper.EnsureSafePath(request.Path);

        if (!string.IsNullOrEmpty(request.Path) && !user.CanAccessFolder(PathHelper.GetFirstSegment(request.Path)))
        {
            return new(TypedResults.NotFound());
        }

        var isRoot = string.IsNullOrEmpty(request.Path);
        var entries = CollectEntries(fileProvider, imageFormats.Value, request.Path, isRoot, user);

        var result = TypedResults.Ok(PaginatedResult<FolderEntry>.Paginate(entries, request.Page, request.PageSize));
        return new(result);
    }

    private static List<FolderEntry> CollectEntries(IFileProvider provider, ImageFormatOptions formats, string relativePath, bool isRoot, IUser currentUser)
    {
        var entries = new List<FolderEntry>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var contents = provider.GetDirectoryContents(relativePath);
        foreach (var item in contents)
        {
            if (item.IsDirectory)
            {
                if (isRoot && !currentUser.CanAccessFolder(item.Name))
                {
                    continue;
                }

                var folderPath = string.IsNullOrEmpty(relativePath) ? item.Name : $"{relativePath}/{item.Name}";
                var folderContents = provider.GetDirectoryContents(folderPath);
                if (!BrowsingHelpers.HasVisibleContent(provider, formats, folderPath, folderContents))
                {
                    continue;
                }

                entries.Add(new FolderEntry { Name = item.Name, Type = EntryType.Folder });
            }
            else if (!isRoot && TryGetVisibleFileName(item.Name, formats, seenFiles, out var fileName))
            {
                entries.Add(new FolderEntry { Name = fileName, Type = EntryType.File });
            }
        }

        entries.Sort(CompareEntries);

        return entries;
    }

    private static bool TryGetVisibleFileName(string name, ImageFormatOptions formats, HashSet<string> seenFiles, out string fileName)
    {
        fileName = "";
        if (ImageConverterJob.IsThumbprintFile(name))
        {
            return false;
        }

        if (!BrowsingHelpers.IsImageFile(name, formats))
        {
            return false;
        }

        fileName = Path.GetFileNameWithoutExtension(name);
        return seenFiles.Add(fileName);
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
