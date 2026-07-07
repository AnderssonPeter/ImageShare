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
            IOptions<ImageFormatOptions> imageFormats,
            IContentTypeProvider contentTypeProvider,
            User user,
            HttpContext context,
            string path,
            bool thumbnail = false) =>
            await ServeImageAsync(fileProvider, imageFormats.Value, contentTypeProvider, user, path, context.Request.Headers.Accept, thumbnail));

        return endpoints;
    }

    internal static async Task<IResult> ServeImageAsync(
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
        var candidates = FindMatchingFiles(fileProvider, imageFormats, directory, baseName, thumbnail);

        if (candidates.Count == 0)
        {
            return Results.NotFound();
        }

        return await ServeBestMatchAsync(candidates, contentTypeProvider, acceptHeader);
    }

    private static List<IFileInfo> FindMatchingFiles(IFileProvider fileProvider, ImageFormatOptions imageFormats, string directory, string baseName, bool thumbnail)
    {
        var candidates = new List<IFileInfo>();
        var contents = fileProvider.GetDirectoryContents(directory);

        baseName = thumbnail ? baseName + ThumbprintOptions.ThumbInfix : baseName;

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

        candidates.Sort((a, b) => a.Length.CompareTo(b.Length));

        return candidates;
    }

    private static async Task<IResult> ServeBestMatchAsync(
        List<IFileInfo> candidates,
        IContentTypeProvider contentTypeProvider,
        StringValues acceptHeader)
    {
        foreach (var file in candidates)
        {
            var mime = contentTypeProvider.GetContentType(Path.GetExtension(file.Name));

            if (IsFormatAccepted(acceptHeader, mime))
            {
                return ServeFile(file, mime);
            }
        }

        return Results.StatusCode(406);
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
