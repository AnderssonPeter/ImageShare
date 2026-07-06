using ImageShare.Authentication;
using ImageShare.Thumbnail;
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

        group.MapGet("/{**path}", async (
            IFileProvider fileProvider,
            IThumbnailService thumbnailService,
            IOptions<ImageFormatOptions> imageFormats,
            IContentTypeProvider contentTypeProvider,
            User user,
            HttpContext context,
            string path,
            bool thumbnail = false) =>
            await ServeImageAsync(fileProvider, thumbnailService, imageFormats.Value, contentTypeProvider, user, path, context.Request.Headers.Accept, thumbnail, context.RequestAborted));

        return endpoints;
    }

    internal static async Task<IResult> ServeImageAsync(
        IFileProvider fileProvider,
        IThumbnailService thumbnailService,
        ImageFormatOptions imageFormats,
        IContentTypeProvider contentTypeProvider,
        IUser user,
        string relativePath,
        StringValues acceptHeader,
        bool thumbnail,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        if (relativePath.Contains("..", StringComparison.Ordinal))
        {
            return Results.BadRequest();
        }

        if (PathHelper.IsInFolder(relativePath) && !user.CanAccessFolder(PathHelper.GetFirstSegment(relativePath)))
        {
            return Results.Forbid();
        }

        var baseName = Path.GetFileNameWithoutExtension(relativePath);

        if (baseName.Contains(ThumbprintOptions.ThumbInfix, StringComparison.Ordinal))
        {
            return Results.BadRequest();
        }

        var directory = Path.GetDirectoryName(relativePath) ?? "";
        var candidates = FindMatchingFiles(fileProvider, directory, baseName);

        if (candidates.Count == 0)
        {
            return Results.NotFound();
        }

        return await ServeBestMatchAsync(candidates, thumbnailService, contentTypeProvider, acceptHeader, thumbnail, cancellationToken);
    }

    private static List<IFileInfo> FindMatchingFiles(IFileProvider fileProvider, ImageFormatOptions imageFormats, string directory, string baseName)
    {
        var candidates = new List<IFileInfo>();
        var contents = fileProvider.GetDirectoryContents(directory);

        foreach (var item in contents)
        {
            if (item.IsDirectory)
            {
                continue;
            }

            var extension = Path.GetExtension(item.Name).TrimStart('.');
            if (string.Equals(Path.GetFileNameWithoutExtension(item.Name), baseName, StringComparison.OrdinalIgnoreCase) && imageFormats.SupportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(item);
            }
        }

        candidates.Sort((a, b) => a.Length.CompareTo(b.Length));

        return candidates;
    }

    private static async Task<IResult> ServeBestMatchAsync(
        List<IFileInfo> candidates,
        IThumbnailService thumbnailService,
        IContentTypeProvider contentTypeProvider,
        StringValues acceptHeader,
        bool thumbnail,
        CancellationToken ct)
    {
        if (thumbnail)
        {
            return await ServeThumbAsync(candidates, thumbnailService, contentTypeProvider, acceptHeader, ct);
        }

        foreach (var file in candidates)
        {
            if (Path.GetFileNameWithoutExtension(file.Name).Contains(ThumbprintOptions.ThumbInfix, StringComparison.Ordinal))
            {
                continue;
            }

            var mime = contentTypeProvider.GetContentType(Path.GetExtension(file.Name));

            if (IsFormatAccepted(acceptHeader, mime))
            {
                return ServeFile(file, mime);
            }
        }

        return Results.StatusCode(406);
    }

    private static async Task<IResult> ServeThumbAsync(
        List<IFileInfo> candidates,
        IThumbnailService thumbnailService,
        IContentTypeProvider contentTypeProvider,
        StringValues acceptHeader,
        CancellationToken ct)
    {
        foreach (var file in candidates)
        {
            var baseName = Path.GetFileNameWithoutExtension(file.Name);

            if (!baseName.Contains(ThumbprintOptions.ThumbInfix, StringComparison.Ordinal))
            {
                continue;
            }

            var mime = contentTypeProvider.GetContentType(Path.GetExtension(file.Name));

            if (IsFormatAccepted(acceptHeader, mime))
            {
                return ServeFile(file, mime);
            }
        }

        var source = candidates.FirstOrDefault(f =>
            !Path.GetFileNameWithoutExtension(f.Name).Contains(ThumbprintOptions.ThumbInfix, StringComparison.Ordinal));

        if (source is null)
        {
            return Results.NotFound();
        }

        var imageData = await ReadAllBytesAsync(source, ct);
        var thumbData = thumbnailService.GenerateThumbnail(imageData);
        var thumbMime = contentTypeProvider.GetContentType(".jpeg");

        if (!IsFormatAccepted(acceptHeader, thumbMime))
        {
            return Results.StatusCode(406);
        }

        return Results.Bytes(thumbData, thumbMime);
    }

    private static async Task<byte[]> ReadAllBytesAsync(IFileInfo fileInfo, CancellationToken ct)
    {
        await using var stream = fileInfo.CreateReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    internal static IResult ServeFile(IFileInfo fileInfo, string mimeType) =>
        Results.Stream(fileInfo.CreateReadStream(), mimeType);

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
}
