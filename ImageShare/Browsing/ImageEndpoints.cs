using System.Diagnostics;
using System.IO.Compression;
using ImageShare.Authentication;
using ImageShare.ImageConversion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/images").RequireAuthorization();

        group.MapGet("/random-thumbnail/{**path}", (
            IFileProvider fileProvider,
            IOptions<ImageFormatOptions> imageFormats,
            IContentTypeProvider contentTypeProvider,
            User user,
            HttpContext context,
            string path) =>
            GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, path, context.Request.Headers.Accept));

        group.MapGet("/download", (
            IFileProvider fileProvider,
            IOptions<ImageFormatOptions> imageFormats,
            User user,
            StringValues folders,
            StringValues format) =>
            DownloadImages(fileProvider, imageFormats.Value, user, folders, format));

        group.MapGet("/random", (
            IFileProvider fileProvider,
            IOptions<ImageFormatOptions> imageFormats,
            IContentTypeProvider contentTypeProvider,
            User user,
            StringValues folders,
            [FromHeader(Name = "Accept")] string? accept) =>
            GetRandomImage(fileProvider, imageFormats.Value, contentTypeProvider, user, folders, new StringValues(accept)));

        group.MapGet("/{**path}", (
            IFileProvider fileProvider,
            IOptions<ImageFormatOptions> imageFormats,
            IContentTypeProvider contentTypeProvider,
            User user,
            HttpContext context,
            string path,
            bool thumbnail = false) =>
            ServeImage(fileProvider, imageFormats.Value, contentTypeProvider, user, path, context.Request.Headers.Accept, thumbnail));

        return endpoints;
    }

    internal static Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult> ServeImage(
        IFileProvider fileProvider,
        ImageFormatOptions imageFormats,
        IContentTypeProvider contentTypeProvider,
        IUser user,
        string relativePath,
        StringValues acceptHeader,
        bool thumbnail)
    {
        if (!user.IsAuthenticated)
        {
            return TypedResults.Unauthorized();
        }

        PathHelper.EnsureSafePath(relativePath);

        if (PathHelper.IsInFolder(relativePath) && !user.CanAccessFolder(PathHelper.GetFirstSegment(relativePath)))
        {
            return TypedResults.Forbid();
        }

        var baseName = Path.GetFileNameWithoutExtension(relativePath);

        if (ImageConverterJob.IsThumbprintFile(baseName))
        {
            return TypedResults.BadRequest();
        }

        var directory = Path.GetDirectoryName(relativePath) ?? "";
        var candidates = FindMatchingFiles(fileProvider, imageFormats, directory, baseName, thumbnail);

        if (candidates.Count == 0)
        {
            return TypedResults.NotFound();
        }

        var bestResult = ServeBestMatch(candidates, contentTypeProvider, acceptHeader);

        return bestResult.Result switch
        {
            FileStreamHttpResult file => file,
            StatusCodeHttpResult status => status,
            _ => throw new UnreachableException("Failed to find a matching result type")
        };
    }

    internal static List<IFileInfo> FindMatchingFiles(IFileProvider fileProvider, ImageFormatOptions imageFormats, string directory, string baseName, bool thumbnail)
    {
        var candidates = new List<IFileInfo>();
        var contents = fileProvider.GetDirectoryContents(directory);

        baseName = thumbnail ? baseName + ImageConverterOptions.ThumbnailInfix : baseName;

        foreach (var item in contents)
        {
            if (item.IsDirectory)
            {
                continue;
            }

            var extension = Path.GetExtension(item.Name).TrimStart('.');
            if (!imageFormats.SupportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Name);
            if (!string.Equals(nameWithoutExtension, baseName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            candidates.Add(item);
        }

        candidates.Sort((left, right) => left.Length.CompareTo(right.Length));

        return candidates;
    }

    internal static Results<FileStreamHttpResult, StatusCodeHttpResult> ServeBestMatch(
        List<IFileInfo> candidates,
        IContentTypeProvider contentTypeProvider,
        StringValues acceptHeader)
    {
        foreach (var file in candidates)
        {
            var mimeType = contentTypeProvider.GetContentType(Path.GetExtension(file.Name));

            if (IsFormatAccepted(acceptHeader, mimeType))
            {
                return ServeFile(file, mimeType);
            }
        }

        return TypedResults.StatusCode(406);
    }

    internal static FileStreamHttpResult ServeFile(IFileInfo fileInfo, string mimeType) =>
        TypedResults.Stream(fileInfo.CreateReadStream(), mimeType);

    internal static bool IsFormatAccepted(StringValues acceptHeader, string mimeType)
    {
        if (StringValues.IsNullOrEmpty(acceptHeader) || acceptHeader.Count == 0)
        {
            return true;
        }

        foreach (var header in acceptHeader)
        {
            if (header is null)
            {
                continue;
            }

            foreach (var segment in header.ToString().Split(','))
            {
                var mediaType = segment.Trim().Split(';')[0].Trim();

                if (string.Equals(mediaType, "*/*", StringComparison.Ordinal) ||
                    string.Equals(mediaType, "image/*", StringComparison.Ordinal) ||
                    string.Equals(mediaType, mimeType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult> GetRandomThumbnail(
        IFileProvider fileProvider,
        ImageFormatOptions imageFormats,
        IContentTypeProvider contentTypeProvider,
        IUser user,
        string relativePath,
        StringValues acceptHeader)
    {
        if (!user.IsAuthenticated)
        {
            return TypedResults.Unauthorized();
        }

        PathHelper.EnsureSafePath(relativePath);

        if (!string.IsNullOrEmpty(relativePath) && !user.CanAccessFolder(PathHelper.GetFirstSegment(relativePath)))
        {
            return TypedResults.Forbid();
        }

        var imageFiles = GetImageBaseNames(fileProvider, relativePath, imageFormats);

        if (imageFiles.Count == 0)
        {
            return TypedResults.NotFound();
        }

        var randomBaseName = imageFiles[Random.Shared.Next(imageFiles.Count)];

        var candidates = FindMatchingFiles(fileProvider, imageFormats, relativePath, randomBaseName, thumbnail: true);

        if (candidates.Count == 0)
        {
            return TypedResults.NotFound();
        }

        var bestResult = ServeBestMatch(candidates, contentTypeProvider, acceptHeader);

        return bestResult.Result switch
        {
            FileStreamHttpResult file => file,
            StatusCodeHttpResult status => status,
            _ => throw new UnreachableException("Failed to find a matching result type")
        };
    }

    private static List<string> GetImageBaseNames(IFileProvider fileProvider, string directory, ImageFormatOptions imageFormats)
    {
        var imageFiles = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in fileProvider.GetDirectoryContents(directory))
        {
            if (!item.Exists || item.IsDirectory)
            {
                continue;
            }

            if (ImageConverterJob.IsThumbprintFile(item.Name))
            {
                continue;
            }

            var extension = Path.GetExtension(item.Name).TrimStart('.');
            if (!imageFormats.SupportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Name);
            if (seen.Add(nameWithoutExtension))
            {
                imageFiles.Add(nameWithoutExtension);
            }
        }

        return imageFiles;
    }

    internal static Results<PushStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound> DownloadImages(
        IFileProvider fileProvider,
        ImageFormatOptions imageFormats,
        IUser user,
        StringValues folderValues,
        StringValues formatValues)
    {
        if (!user.IsAuthenticated)
        {
            return TypedResults.Unauthorized();
        }

        var folders = NormalizeFolders(folderValues);
        if (folders.Count == 0)
        {
            return TypedResults.BadRequest();
        }

        var format = NormalizeFormat(formatValues);
        if (format is not null && !imageFormats.SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.BadRequest();
        }

        foreach (var folder in folders)
        {
            PathHelper.EnsureSafePath(folder);
            if (!user.CanAccessFolder(PathHelper.GetFirstSegment(folder)))
            {
                return TypedResults.Forbid();
            }
        }

        var imageFiles = folders
            .SelectMany(folder => EnumerateImageFiles(fileProvider, imageFormats, folder))
            .Where(file => format is null || string.Equals(Path.GetExtension(file.Info.Name).TrimStart('.'), format, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (imageFiles.Count == 0)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Stream(async stream => await WriteZipAsync(imageFiles, stream, CancellationToken.None), "application/zip", "images.zip");
    }

    internal static async Task WriteZipAsync(IEnumerable<(string Path, IFileInfo Info)> imageFiles, Stream output, CancellationToken cancellationToken)
    {
        await using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var (path, info) in imageFiles)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
            await using var entryStream = await entry.OpenAsync(cancellationToken);
            await using var fileStream = info.CreateReadStream();
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }
    }

    internal static Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult> GetRandomImage(
        IFileProvider fileProvider,
        ImageFormatOptions imageFormats,
        IContentTypeProvider contentTypeProvider,
        IUser user,
        StringValues folderValues,
        StringValues acceptHeader)
    {
        if (!user.IsAuthenticated)
        {
            return TypedResults.Unauthorized();
        }

        var folders = NormalizeFolders(folderValues);
        if (folders.Count == 0)
        {
            return TypedResults.BadRequest();
        }

        foreach (var folder in folders)
        {
            PathHelper.EnsureSafePath(folder);
            if (!user.CanAccessFolder(PathHelper.GetFirstSegment(folder)))
            {
                return TypedResults.Forbid();
            }
        }

        var baseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            foreach (var (_, info) in EnumerateImageFiles(fileProvider, imageFormats, folder))
            {
                baseNames.Add(Path.GetFileNameWithoutExtension(info.Name));
            }
        }

        if (baseNames.Count == 0)
        {
            return TypedResults.NotFound();
        }

        var baseNamesList = baseNames.ToList();
        var randomBaseName = baseNamesList[Random.Shared.Next(baseNamesList.Count)];

        var candidates = new List<IFileInfo>();
        foreach (var folder in folders)
        {
            foreach (var (_, info) in EnumerateImageFiles(fileProvider, imageFormats, folder))
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(info.Name), randomBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(info);
                }
            }
        }

        candidates.Sort((left, right) => left.Length.CompareTo(right.Length));

        var bestResult = ServeBestMatch(candidates, contentTypeProvider, acceptHeader);

        return bestResult.Result switch
        {
            FileStreamHttpResult file => file,
            StatusCodeHttpResult status => status,
            _ => throw new UnreachableException("Failed to find a matching result type")
        };
    }

    private static List<string> NormalizeFolders(StringValues folderValues)
    {
        var folders = new List<string>(folderValues.Count);
        foreach (var folder in folderValues)
        {
            if (!string.IsNullOrWhiteSpace(folder))
            {
                folders.Add(folder);
            }
        }

        return folders;
    }

    private static string? NormalizeFormat(StringValues formatValues)
    {
        foreach (var format in formatValues)
        {
            if (!string.IsNullOrWhiteSpace(format))
            {
                return format.Trim().TrimStart('.').ToLowerInvariant();
            }
        }

        return null;
    }

    internal static IEnumerable<(string Path, IFileInfo Info)> EnumerateImageFiles(IFileProvider fileProvider, ImageFormatOptions imageFormats, string folder)
    {
        foreach (var item in fileProvider.GetDirectoryContents(folder))
        {
            if (!item.Exists)
            {
                continue;
            }

            var itemPath = string.IsNullOrEmpty(folder) ? item.Name : $"{folder}/{item.Name}";

            if (item.IsDirectory)
            {
                foreach (var nested in EnumerateImageFiles(fileProvider, imageFormats, itemPath))
                {
                    yield return nested;
                }

                continue;
            }

            if (BrowsingEndpoints.IsHiddenFile(item.Name))
            {
                continue;
            }

            if (ImageConverterJob.IsThumbprintFile(item.Name))
            {
                continue;
            }

            if (!BrowsingEndpoints.IsImageFile(item.Name, imageFormats))
            {
                continue;
            }

            yield return (itemPath, item);
        }
    }
}
