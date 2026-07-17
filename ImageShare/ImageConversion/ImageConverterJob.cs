using ImageShare.Browsing;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;

namespace ImageShare.ImageConversion;

internal sealed class ImageConverterJob(
    IWritableFileProvider fileProvider,
    ImageConverter converter,
    IOptions<ImageFormatOptions> imageFormats,
    ILogger<ImageConverterJob> logger) : BackgroundService
{
    private readonly IWritableFileProvider _fileProvider = fileProvider;
    private readonly ImageConverter _converter = converter;
    private readonly ImageFormatOptions _imageFormats = imageFormats.Value;
    private readonly ILogger<ImageConverterJob> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ScanAndConvertAsync(stoppingToken);

        if (!stoppingToken.IsCancellationRequested)
        {
            await WatchForChangesAsync(stoppingToken);
        }
    }

    private async Task WatchForChangesAsync(CancellationToken cancellationToken)
    {
        var changeToken = _fileProvider.Watch("**/*");

        var reset = new SemaphoreSlim(0, 1);
        using var changeRegistration = changeToken.RegisterChangeCallback(_ => reset.Release(), null);
        while (!cancellationToken.IsCancellationRequested)
        {
            await reset.WaitAsync(cancellationToken);

            try
            {
                await ScanAndConvertAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Image conversion scan canceled");
            }
        }
    }

    internal async Task ScanAndConvertAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting image conversion scan");

        var imageFiles = EnumerateAllFiles("")
            .Where(file => IsImageFile(file) && !IsThumbprintFile(file));

        foreach (var file in imageFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ConvertImageAsync(file, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert image {File}", file);
            }
        }

        _logger.LogInformation("Image conversion scan complete");
    }

    internal async Task ConvertImageAsync(string imagePath, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(imagePath).TrimStart('.');
        var sourceFormat = ImageConverter.ParseFormat(extension);

        var imageData = await _fileProvider.GetFileInfo(imagePath).ReadAsBytesAsync();
        var directory = Path.GetDirectoryName(imagePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(imagePath);

        foreach (var format in _imageFormats.SupportedFormats)
        {
            var targetFormat = ImageConverter.ParseFormat(format);
            if (targetFormat == sourceFormat)
            {
                continue;
            }

            var targetExtension = format.ToLowerInvariant() switch
            {
                "jpg" => ".jpg",
                "jpeg" => ".jpg",
                _ => $".{format.ToLowerInvariant()}",
            };

            var fullPath = PathHelper.Combine(directory, $"{name}{targetExtension}");
            if (!_fileProvider.GetFileInfo(fullPath).Exists)
            {
                try
                {
                    _logger.LogInformation("Converting {Source} to full-resolution {Format}", imagePath, format);
                    var fullData = _converter.ConvertFull(imageData, targetFormat);
                    await _fileProvider.WriteAsync(fullPath, fullData.ToArray(), cancel: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert {Source} to full {Format}", imagePath, format);
                }
            }

            var thumbnailName = $"{name}{ImageConverterOptions.ThumbnailInfix}{targetExtension}";
            var thumbnailPath = PathHelper.Combine(directory, thumbnailName);
            if (!_fileProvider.GetFileInfo(thumbnailPath).Exists)
            {
                try
                {
                    _logger.LogInformation("Converting {Source} to thumbnail {Format}", imagePath, format);
                    var thumbnailData = _converter.ConvertThumbnail(imageData, targetFormat);
                    await _fileProvider.WriteAsync(thumbnailPath, thumbnailData.ToArray(), cancel: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert {Source} to thumbnail {Format}", imagePath, format);
                }
            }
        }
    }

    internal bool IsImageFile(string path)
    {
        var extension = Path.GetExtension(path).TrimStart('.');
        return _imageFormats.SupportedFormats.Any(format => string.Equals(extension, format, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsThumbprintFile(string path)
    {
        var name = Path.GetFileName(path);
        return name.Contains(ImageConverterOptions.ThumbnailInfix, StringComparison.Ordinal);
    }

    private IEnumerable<string> EnumerateAllFiles(string subpath)
    {
        foreach (var item in _fileProvider.GetDirectoryContents(subpath))
        {
            if (!item.Exists)
            {
                continue;
            }

            var itemPath = string.IsNullOrEmpty(subpath) ? item.Name : $"{subpath}/{item.Name}";

            if (item.IsDirectory)
            {
                foreach (var nestedFile in EnumerateAllFiles(itemPath))
                {
                    yield return nestedFile;
                }
            }
            else
            {
                yield return itemPath;
            }
        }
    }
}
