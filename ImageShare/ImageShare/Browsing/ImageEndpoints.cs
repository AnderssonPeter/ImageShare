using System.Diagnostics;
using ImageShare.Authentication;
using ImageShare.ImageConversion;
using Microsoft.AspNetCore.Http.HttpResults;
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

        if (relativePath.Contains("..", StringComparison.Ordinal))
        {
            return TypedResults.BadRequest();
        }

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

    private static List<IFileInfo> FindMatchingFiles(IFileProvider fileProvider, ImageFormatOptions imageFormats, string directory, string baseName, bool thumbnail)
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

    private static Results<FileStreamHttpResult, StatusCodeHttpResult> ServeBestMatch(
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
}
