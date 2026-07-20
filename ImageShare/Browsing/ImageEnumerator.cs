using ImageShare.ImageConversion;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ImageShare.Browsing;

public sealed class ImageEnumerator(IFileProvider fileProvider, IOptions<ImageFormatOptions> imageFormats)
{
    private readonly IFileProvider _fileProvider = fileProvider;
    private readonly ImageFormatOptions _imageFormats = imageFormats.Value;

    public IReadOnlyList<string> SupportedFormats => _imageFormats.SupportedFormats;

    public bool IsImageFile(string path)
    {
        var relativePath = new RelativePath(path);
        return relativePath.HasExtension && _imageFormats.SupportedFormats.Contains(relativePath.Extension, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsSupportedFormat(string format) =>
        _imageFormats.SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase);

    public bool IsHiddenFile(string name) => name.StartsWith('.');

    public bool IsThumbnailFile(string path)
    {
        var relativePath = new RelativePath(path);
        return relativePath.IsThumbnail;
    }

    public string BuildThumbnailName(string baseName) => baseName + ImageConverterOptions.ThumbnailInfix;

    public IEnumerable<(RelativePath Path, IFileInfo Info)> EnumerateImages(string directory, bool recursive = false, bool thumbnails = false)
    {
        var basePath = new RelativePath(directory);
        foreach (var item in _fileProvider.GetDirectoryContents(directory))
        {
            if (!item.Exists)
            {
                continue;
            }

            var itemPath = basePath.IsEmpty ? new RelativePath(item.Name) : basePath.Combine(item.Name);

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

            var relativePath = new RelativePath(itemPath);
            var isThumbnail = relativePath.IsThumbnail;
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

    public bool HasVisibleContent(string directory) => EnumerateImages(directory, recursive: true).Any();

    public IDirectoryContents GetDirectoryContents(string path) => _fileProvider.GetDirectoryContents(path);

    public IReadOnlyList<IFileInfo> FindMatchingFiles(string directory, string baseName, bool thumbnail)
    {
        var targetBaseName = thumbnail ? BuildThumbnailName(baseName) : baseName;
        return [.. EnumerateImages(directory, thumbnails: thumbnail)
            .Where(file => string.Equals(file.Path.FileNameWithoutExtension, targetBaseName, StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Info)
            .OrderBy(file => file.Length)];
    }
}
