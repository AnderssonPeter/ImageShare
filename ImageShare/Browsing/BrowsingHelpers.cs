using System.IO.Compression;
using ImageShare.ImageConversion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

internal static class BrowsingHelpers
{
    public static Results<FileStreamHttpResult, StatusCodeHttpResult> ServeBestMatch(
        IReadOnlyList<IFileInfo> candidates,
        IContentTypeProvider contentTypeProvider,
        StringValues acceptHeader)
    {
        foreach (var file in candidates)
        {
            var filePath = new RelativePath(file.Name);
            var mimeType = contentTypeProvider.GetContentType($".{filePath.Extension}");

            if (IsFormatAccepted(acceptHeader, mimeType))
            {
                return TypedResults.Stream(file.CreateReadStream(), mimeType);
            }
        }

        return TypedResults.StatusCode(406);
    }

    public static bool IsFormatAccepted(StringValues acceptHeader, string mimeType)
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

    public static async Task WriteZipAsync(IEnumerable<(RelativePath Path, IFileInfo Info)> imageFiles, Stream output, CancellationToken cancellationToken)
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

    public static List<string> NormalizeFolders(StringValues folderValues) =>
        [.. folderValues.Where(value => !string.IsNullOrEmpty(value)).Cast<string>()];

    public static string? NormalizeFormat(StringValues formatValues) =>
        formatValues.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?
            .Trim().TrimStart('.').ToLowerInvariant();
}
