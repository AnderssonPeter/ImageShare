using ImageShare.Authentication;
using Microsoft.Extensions.Options;

namespace ImageShare.Browsing;

public static class BrowsingEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    public static IEndpointRouteBuilder MapFolderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/folders").RequireAuthorization();

        group.MapGet("/", (IOptions<StorageOptions> storageOptions, User user, int page = DefaultPage, int pageSize = DefaultPageSize) =>
            GetEntries(storageOptions.Value.BasePath, string.Empty, user, page, pageSize));

        group.MapGet("/{**path}", (IOptions<StorageOptions> storageOptions, User user, string path, int page = DefaultPage, int pageSize = DefaultPageSize) =>
            GetEntries(storageOptions.Value.BasePath, path, user, page, pageSize));

        return endpoints;
    }

    internal static IResult GetEntries(string basePath, string relativePath, IUser user, int page, int pageSize)
    {
        if (!user.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return Results.BadRequest();
        }

        var targetPath = Path.GetFullPath(Path.Combine(basePath, relativePath));

        if (!targetPath.StartsWith(Path.GetFullPath(basePath), StringComparison.Ordinal))
        {
            return Results.BadRequest();
        }

        if (!Directory.Exists(targetPath))
        {
            return Results.NotFound();
        }

        var isRoot = string.IsNullOrEmpty(relativePath);
        var entries = CollectEntries(targetPath, isRoot, user);

        return Results.Ok(Paginate(entries, page, pageSize));
    }

    private static List<FolderEntry> CollectEntries(string targetPath, bool isRoot, IUser user)
    {
        var entries = new List<FolderEntry>();

        foreach (var dir in Directory.EnumerateDirectories(targetPath))
        {
            var name = Path.GetFileName(dir);

            if (isRoot && !user.CanAccessFolder(name))
            {
                continue;
            }

            entries.Add(new FolderEntry { Name = name, Type = EntryType.Folder });
        }

        foreach (var file in Directory.EnumerateFiles(targetPath))
        {
            entries.Add(new FolderEntry { Name = Path.GetFileName(file), Type = EntryType.File });
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

    private static PaginatedResult<FolderEntry> Paginate(List<FolderEntry> entries, int page, int pageSize)
    {
        var totalCount = entries.Count;
        var paged = entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<FolderEntry>
        {
            Items = paged,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
