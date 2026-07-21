using ImageShare.ImageConversion;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ImageShare.Browsing;

public sealed class ImageEnumerator(IFileProvider fileProvider, IOptions<ImageFormatOptions> imageFormats)
{

    public IReadOnlyList<string> SupportedFormats => imageFormats.Value.SupportedFormats;

    public bool IsImageFile(string path)
    {
        var relativePath = new RelativePath(path);
        return relativePath.HasExtension && imageFormats.Value.SupportedFormats.Contains(relativePath.Extension, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsSupportedFormat(string format) =>
        imageFormats.Value.SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase);

    public bool IsHiddenFile(string name) => name.StartsWith('.');

    public bool IsThumbnailFile(string path)
    {
        var relativePath = new RelativePath(path);
        return relativePath.IsThumbnail;
    }

    public string BuildThumbnailName(string baseName) => baseName + ImageConverterOptions.ThumbnailInfix;

    public IEnumerable<(RelativePath Path, IFileInfo Info)> EnumerateImages(RelativePath directory, bool recursive = false, bool thumbnails = false)
    {
        foreach (var item in fileProvider.GetDirectoryContents(directory))
        {
            if (!item.Exists)
            {
                continue;
            }

            var itemPath = directory.Combine(item.Name);

            if (item.IsDirectory)
            {
                if (recursive && !IsHiddenFile(item.Name))
                {
                    foreach (var nested in EnumerateImages(itemPath, recursive, thumbnails))
                    {
                        yield return nested;
                    }
                }

                continue;
            }

            if (IsHiddenFile(item.Name))
            {
                continue;
            }

            var isThumbnail = itemPath.IsThumbnail;
            if (thumbnails != isThumbnail)
            {
                continue;
            }

            if (!IsImageFile(item.Name))
            {
                continue;
            }

            yield return (itemPath, item);
        }
    }

    public bool HasVisibleContent(RelativePath directory) => EnumerateImages(directory, recursive: true).Any();

    public IDirectoryContents GetDirectoryContents(RelativePath path) => fileProvider.GetDirectoryContents(path);

    public IReadOnlyList<IFileInfo> FindMatchingFiles(RelativePath directory, string baseName, bool thumbnail)
    {
        var targetBaseName = thumbnail ? BuildThumbnailName(baseName) : baseName;
        return [.. EnumerateImages(directory, thumbnails: thumbnail)
            .Where(file => string.Equals(file.Path.FileNameWithoutExtension, targetBaseName, StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Info)
            .OrderBy(file => file.Length)];
    }
}
