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
        logger.LogInformation("Starting initial image conversion scan");
        await ScanAndConvertAsync(stoppingToken);
        logger.LogInformation("Initial image conversion scan complete");
        if (!stoppingToken.IsCancellationRequested)
        {
            await WatchForChangesAsync(stoppingToken);
        }
    }

    private async Task WatchForChangesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Waiting for file changes to trigger image conversion scan");
            await WaitForChangeOnceAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            logger.LogInformation("File change detected, starting image conversion scan");
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

    private async Task WaitForChangeOnceAsync(CancellationToken cancellationToken)
    {
        var reset = new SemaphoreSlim(0, 1);
        var changeToken = fileProvider.Watch("**/*");
        using var changeRegistration = changeToken.RegisterChangeCallback(_ => reset.Release(), null);
        await reset.WaitAsync(cancellationToken);
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
            var isSourceFormat = targetFormat == sourceFormat;

            var targetExtension = format.ToLowerInvariant() switch
            {
                "jpg" => ".jpg",
                "jpeg" => ".jpg",
                _ => $".{format.ToLowerInvariant()}",
            };

            var fullPath = directory.Combine($"{name}{targetExtension}");
            if (!isSourceFormat && !fileProvider.GetFileInfo(fullPath).Exists)
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
