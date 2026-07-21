using ImageShare.Browsing;
using Mirality.FileProviders;

namespace ImageShare.ImageConversion;

internal sealed class ImageConverterJob(
    IWritableFileProvider fileProvider,
    ImageEnumerator imageEnumerator,
    ImageConverter converter,
    ILogger<ImageConverterJob> logger) : BackgroundService
{
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
        var changeToken = fileProvider.Watch("**/*");

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
                logger.LogInformation(ex, "Image conversion scan canceled");
            }
        }
    }

    internal async Task ScanAndConvertAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting image conversion scan");

        var imageFiles = imageEnumerator.EnumerateImages(RelativePath.Root, recursive: true)
            .Select(file => file.Path);

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
                logger.LogWarning(ex, "Failed to convert image {File}", file);
            }
        }

        logger.LogInformation("Image conversion scan complete");
    }

    internal async Task ConvertImageAsync(RelativePath imagePath, CancellationToken cancellationToken)
    {
        var sourceFormat = ImageConverter.ParseFormat(imagePath.Extension!);
        var directory = imagePath.Directory;
        var name = imagePath.FileNameWithoutExtension;

        var imageData = await fileProvider.GetFileInfo(imagePath).ReadAsBytesAsync();

        foreach (var format in imageEnumerator.SupportedFormats)
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

            var fullPath = directory.Combine($"{name}{targetExtension}");
            if (!fileProvider.GetFileInfo(fullPath).Exists)
            {
                try
                {
                    logger.LogInformation("Converting {Source} to full-resolution {Format}", imagePath, format);
                    var fullData = converter.ConvertFull(imageData, targetFormat);
                    await fileProvider.WriteAsync(fullPath, fullData.ToArray(), cancel: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to convert {Source} to full {Format}", imagePath, format);
                }
            }

            var thumbnailName = $"{name}{ImageConverterOptions.ThumbnailInfix}{targetExtension}";
            var thumbnailPath = directory.Combine(thumbnailName);
            if (!fileProvider.GetFileInfo(thumbnailPath).Exists)
            {
                try
                {
                    logger.LogInformation("Converting {Source} to thumbnail {Format}", imagePath, format);
                    var thumbnailData = converter.ConvertThumbnail(imageData, targetFormat);
                    await fileProvider.WriteAsync(thumbnailPath, thumbnailData.ToArray(), cancel: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to convert {Source} to thumbnail {Format}", imagePath, format);
                }
            }
        }
    }
}
