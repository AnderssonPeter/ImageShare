using ImageShare.Authentication;
using ImageShare.Thumbnail;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

public static class ImageEndpoints
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    static ImageEndpoints() => ContentTypeProvider.Mappings[".avif"] = "image/avif";

    private static readonly string[] PreferredConvertFormats = ["jpeg", "png", "webp"];

    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/images").RequireAuthorization();

        group.MapGet("/{**path}", async (
            IFileProvider fileProvider,
            IThumbnailService thumbnailService,
            User user,
            HttpContext context,
            string path) => await ServeImageAsync(fileProvider, thumbnailService, user, path, context.Request.Headers.Accept, context.RequestAborted));

        return endpoints;
    }

    internal static async Task<IResult> ServeImageAsync(
        IFileProvider fileProvider,
        IThumbnailService thumbnailService,
        IUser user,
        string relativePath,
        StringValues acceptHeader,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        if (PathHelper.IsInFolder(relativePath) && !user.CanAccessFolder(PathHelper.GetFirstSegment(relativePath)))
        {
            return Results.NotFound();
        }

        var fileInfo = fileProvider.GetFileInfo(relativePath);

        if (!fileInfo.Exists || fileInfo.IsDirectory)
        {
            return Results.NotFound();
        }

        if (Path.GetFileNameWithoutExtension(relativePath).Contains(ThumbprintOptions.ThumbInfix, StringComparison.Ordinal))
        {
            return Results.NotFound();
        }

        var originalMime = GetMimeType(Path.GetExtension(relativePath));

        if (originalMime is not null && IsFormatAccepted(acceptHeader, originalMime))
        {
            return ServeFile(fileInfo, originalMime);
        }

        var thumbResult = TryServeThumb(fileProvider, relativePath, acceptHeader);
        if (thumbResult is not null)
        {
            return thumbResult;
        }

        return await ServeConverted(fileInfo, relativePath, thumbnailService, acceptHeader, cancellationToken);
    }

    private static IResult? TryServeThumb(IFileProvider fileProvider, string relativePath, StringValues acceptHeader)
    {
        var dir = Path.GetDirectoryName(relativePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var thumbRelPath = string.IsNullOrEmpty(dir)
            ? $"{name}{ThumbprintOptions.ThumbInfix}.jpg"
            : $"{dir}/{name}{ThumbprintOptions.ThumbInfix}.jpg";

        var thumbInfo = fileProvider.GetFileInfo(thumbRelPath);

        if (thumbInfo.Exists && IsFormatAccepted(acceptHeader, "image/jpeg"))
        {
            return ServeFile(thumbInfo, "image/jpeg");
        }

        return null;
    }

    private static async Task<IResult> ServeConverted(
        IFileInfo fileInfo,
        string relativePath,
        IThumbnailService thumbnailService,
        StringValues acceptHeader,
        CancellationToken ct)
    {
        await using var stream = fileInfo.CreateReadStream();
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var imageData = ms.ToArray();

        foreach (var format in PreferredConvertFormats)
        {
            var mime = GetMimeType($".{format}")!;
            if (IsFormatAccepted(acceptHeader, mime))
            {
                var thumbData = thumbnailService.GenerateThumbnail(imageData, new ThumbnailOptions { OutputFormat = format });
                return Results.Bytes(thumbData, mime);
            }
        }

        return Results.Bytes(imageData, GetMimeType(Path.GetExtension(relativePath)) ?? "application/octet-stream");
    }

    internal static IResult ServeFile(IFileInfo fileInfo, string mimeType) =>
        Results.Stream(fileInfo.CreateReadStream(), mimeType);

    internal static string? GetMimeType(string extension) =>
        ContentTypeProvider.TryGetContentType(extension, out var mime) ? mime : null;

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

