using System.IO.Compression;
using ImageShare.ImageConversion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

internal static class BrowsingHelpers
{
    public static bool IsImageFile(string path, ImageFormatOptions imageFormats)
    {
        var extension = Path.GetExtension(path).TrimStart('.');
        return imageFormats.SupportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsHiddenFile(string name) => name.StartsWith('.');

    public static bool HasVisibleContent(IFileProvider fileProvider, ImageFormatOptions imageFormats, string folderPath, IDirectoryContents folderContents)
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

    public static List<IFileInfo> FindMatchingFiles(IFileProvider fileProvider, ImageFormatOptions imageFormats, string directory, string baseName, bool thumbnail)
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

    public static List<IFileInfo> FindMatchingFilesRecursive(IFileProvider fileProvider, ImageFormatOptions imageFormats, string directory, string baseName, bool thumbnail)
    {
        var candidates = new List<IFileInfo>();
        var targetBaseName = thumbnail ? baseName + ImageConverterOptions.ThumbnailInfix : baseName;
        CollectMatchingFilesRecursive(fileProvider, imageFormats, directory, targetBaseName, candidates);
        candidates.Sort((left, right) => left.Length.CompareTo(right.Length));
        return candidates;
    }

    private static void CollectMatchingFilesRecursive(IFileProvider fileProvider, ImageFormatOptions imageFormats, string directory, string targetBaseName, List<IFileInfo> candidates)
    {
        foreach (var item in fileProvider.GetDirectoryContents(directory))
        {
            if (!item.Exists)
            {
                continue;
            }

            if (item.IsDirectory)
            {
                var subPath = string.IsNullOrEmpty(directory) ? item.Name : $"{directory}/{item.Name}";
                CollectMatchingFilesRecursive(fileProvider, imageFormats, subPath, targetBaseName, candidates);
                continue;
            }

            var extension = Path.GetExtension(item.Name).TrimStart('.');
            if (!imageFormats.SupportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Name);
            if (!string.Equals(nameWithoutExtension, targetBaseName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            candidates.Add(item);
        }
    }

    public static Results<FileStreamHttpResult, StatusCodeHttpResult> ServeBestMatch(
        List<IFileInfo> candidates,
        IContentTypeProvider contentTypeProvider,
        StringValues acceptHeader)
    {
        foreach (var file in candidates)
        {
            var mimeType = contentTypeProvider.GetContentType(Path.GetExtension(file.Name));

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

    public static IEnumerable<(string Path, IFileInfo Info)> EnumerateImageFiles(IFileProvider fileProvider, ImageFormatOptions imageFormats, string folder)
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

            if (IsHiddenFile(item.Name))
            {
                continue;
            }

            if (ImageConverterJob.IsThumbprintFile(item.Name))
            {
                continue;
            }

            if (!IsImageFile(item.Name, imageFormats))
            {
                continue;
            }

            yield return (itemPath, item);
        }
    }

    public static async Task WriteZipAsync(IEnumerable<(string Path, IFileInfo Info)> imageFiles, Stream output, CancellationToken cancellationToken)
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

    public static List<string> GetImageBaseNames(IFileProvider fileProvider, string directory, ImageFormatOptions imageFormats)
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
}
