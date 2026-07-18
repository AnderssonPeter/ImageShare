using ImageShare.Authentication;
using ImageShare.ImageConversion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
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

        group.MapGet("/", (IFileProvider fileProvider, IOptions<ImageFormatOptions> imageFormats, User user, int page = DefaultPage, int pageSize = DefaultPageSize) =>
            GetEntries(fileProvider, imageFormats.Value, string.Empty, user, page, pageSize));

        group.MapGet("/{**path}", (IFileProvider fileProvider, IOptions<ImageFormatOptions> imageFormats, User user, string path, int page = DefaultPage, int pageSize = DefaultPageSize) =>
            GetEntries(fileProvider, imageFormats.Value, path, user, page, pageSize));

        return endpoints;
    }

    internal static Results<Ok<PaginatedResult<FolderEntry>>, UnauthorizedHttpResult, BadRequest, NotFound> GetEntries(IFileProvider fileProvider, ImageFormatOptions imageFormats, string relativePath, IUser user, int page, int pageSize)
    {
        if (!user.IsAuthenticated)
        {
            return TypedResults.Unauthorized();
        }

        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return TypedResults.BadRequest();
        }

        PathHelper.EnsureSafePath(relativePath);

        if (!string.IsNullOrEmpty(relativePath) && !user.CanAccessFolder(PathHelper.GetFirstSegment(relativePath)))
        {
            return TypedResults.NotFound();
        }

        var isRoot = string.IsNullOrEmpty(relativePath);
        var entries = CollectEntries(fileProvider, imageFormats, relativePath, isRoot, user);

        return TypedResults.Ok(PaginatedResult<FolderEntry>.Paginate(entries, page, pageSize));
    }

    private static List<FolderEntry> CollectEntries(IFileProvider fileProvider, ImageFormatOptions imageFormats, string relativePath, bool isRoot, IUser user)
    {
        var entries = new List<FolderEntry>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var contents = fileProvider.GetDirectoryContents(relativePath);
        foreach (var item in contents)
        {
            if (item.IsDirectory)
            {
                if (isRoot && !user.CanAccessFolder(item.Name))
                {
                    continue;
                }

                var folderPath = string.IsNullOrEmpty(relativePath) ? item.Name : $"{relativePath}/{item.Name}";
                var folderContents = fileProvider.GetDirectoryContents(folderPath);
                if (!HasVisibleContent(fileProvider, imageFormats, folderPath, folderContents))
                {
                    continue;
                }

                entries.Add(new FolderEntry { Name = item.Name, Type = EntryType.Folder });
            }
            else if (!isRoot && TryGetVisibleFileName(item.Name, imageFormats, seenFiles, out var fileName))
            {
                entries.Add(new FolderEntry { Name = fileName, Type = EntryType.File });
            }
        }

        entries.Sort(CompareEntries);

        return entries;
    }

    private static bool TryGetVisibleFileName(string name, ImageFormatOptions imageFormats, HashSet<string> seenFiles, out string fileName)
    {
        fileName = "";
        if (ImageConverterJob.IsThumbprintFile(name))
        {
            return false;
        }

        if (!IsImageFile(name, imageFormats))
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

    private static bool HasVisibleContent(IFileProvider fileProvider, ImageFormatOptions imageFormats, string folderPath, IDirectoryContents folderContents)
    {
        foreach (var item in folderContents)
        {
            if (!item.Exists)
            {
                continue;
            }

            if (item.IsDirectory)
            {
                var nestedPath = string.IsNullOrEmpty(folderPath) ? item.Name : $"{folderPath}/{item.Name}";
                var nestedContents = fileProvider.GetDirectoryContents(nestedPath);
                if (HasVisibleContent(fileProvider, imageFormats, nestedPath, nestedContents))
                {
                    return true;
                }

                continue;
            }

            if (IsHiddenFile(item.Name))
            {
                continue;
            }

            if (ImageConverterJob.IsThumbprintFile(item.Name))
            {
                continue;
            }

            if (IsImageFile(item.Name, imageFormats))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsImageFile(string path, ImageFormatOptions imageFormats)
    {
        var extension = Path.GetExtension(path).TrimStart('.');
        return imageFormats.SupportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    internal static bool IsHiddenFile(string name) => name.StartsWith('.');
}
