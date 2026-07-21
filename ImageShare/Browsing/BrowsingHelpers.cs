using ImageShare.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

internal static class BrowsingHelpers
{
    public static FileStreamHttpResult ServeBestMatch(
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

        throw new NotAcceptableException("None of the available image formats match the requested Accept header.");
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

    public static string? NormalizeFormat(string[]? formatValues) => formatValues?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?
            .Trim().TrimStart('.').ToLowerInvariant();
}
