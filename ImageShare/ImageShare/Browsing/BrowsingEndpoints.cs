using ImageShare.Authentication;
using ImageShare.Thumbnail;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;

namespace ImageShare.Browsing;

public static class BrowsingEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    public static IEndpointRouteBuilder MapFolderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/folders").RequireAuthorization();

        group.MapGet("/", (IFileProvider fileProvider, User user, int page = DefaultPage, int pageSize = DefaultPageSize) =>
            GetEntries(fileProvider, string.Empty, user, page, pageSize));

        group.MapGet("/{**path}", (IFileProvider fileProvider, User user, string path, int page = DefaultPage, int pageSize = DefaultPageSize) =>
            GetEntries(fileProvider, path, user, page, pageSize));

        return endpoints;
    }

    internal static Results<Ok<PaginatedResult<FolderEntry>>, UnauthorizedHttpResult, BadRequest, NotFound> GetEntries(IFileProvider fileProvider, string relativePath, IUser user, int page, int pageSize)
    {
        if (!user.IsAuthenticated)
        {
            return TypedResults.Unauthorized();
        }

        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return TypedResults.BadRequest();
        }

        if (relativePath.Contains("..", StringComparison.Ordinal))
        {
            return TypedResults.BadRequest();
        }

        if (!string.IsNullOrEmpty(relativePath) && !user.CanAccessFolder(PathHelper.GetFirstSegment(relativePath)))
        {
            return TypedResults.NotFound();
        }

        var contents = fileProvider.GetDirectoryContents(relativePath);

        var isRoot = string.IsNullOrEmpty(relativePath);
        var entries = CollectEntries(contents, isRoot, user);

        return TypedResults.Ok(PaginatedResult<FolderEntry>.Paginate(entries, page, pageSize));
    }

    private static List<FolderEntry> CollectEntries(IDirectoryContents contents, bool isRoot, IUser user)
    {
        var entries = new List<FolderEntry>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in contents)
        {
            var name = item.Name;

            if (item.IsDirectory)
            {
                if (isRoot && !user.CanAccessFolder(name))
                {
                    continue;
                }

                entries.Add(new FolderEntry { Name = name, Type = EntryType.Folder });
            }
            else
            {
                if (isRoot)
                {
                    continue;
                }

                if (name.Contains(ThumbprintOptions.ThumbInfix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
                if (!seenFiles.Add(nameWithoutExtension))
                {
                    continue;
                }

                entries.Add(new FolderEntry { Name = nameWithoutExtension, Type = EntryType.File });
            }
        }

        entries.Sort((a, b) =>
        {
            if (a.Type != b.Type)
            {
                return a.Type == EntryType.Folder ? -1 : 1;
            }

            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        return entries;
    }
}
