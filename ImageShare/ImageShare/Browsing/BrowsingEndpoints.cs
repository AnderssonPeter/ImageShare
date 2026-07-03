using ImageShare.Authentication;
using Microsoft.Extensions.Options;

namespace ImageShare.Browsing;

public static class BrowsingEndpoints
{
    public static IEndpointRouteBuilder MapFolderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/folders").RequireAuthorization();

        group.MapGet("/", (IOptions<StorageOptions> storageOptions, User user) =>
            ListFolder(storageOptions.Value.BasePath, string.Empty, user));

        group.MapGet("/{**path}", (IOptions<StorageOptions> storageOptions, User user, string path) =>
            ListFolder(storageOptions.Value.BasePath, path, user));

        return endpoints;
    }

    internal static IResult ListFolder(string basePath, string relativePath, IUser user)
    {
        if (!user.IsAuthenticated)
        {
            return Results.Unauthorized();
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

        return Results.Ok(entries);
    }
}
