using ImageMagick;
using ImageShare.Browsing;
using ImageShare.ImageConversion;
using Microsoft.Extensions.Logging;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageConverterJobTests(ISyncWritableFileProvider fileProvider, ImageEnumerator imageEnumerator, ImageConverter converter, ILoggerFactory loggerFactory, TestImageFactory imageFactory)
{
    private readonly ImageConverterJob job = new(
            fileProvider,
            imageEnumerator,
            converter,
            loggerFactory.CreateLogger<ImageConverterJob>());

    private static readonly ImageConverterOptions defaultConverterOptions = new()
    {
        FullQuality = 80,
        ThumbnailQuality = 70,
        ThumbnailMaxWidth = 200,
        ThumbnailMaxHeight = 200,
    };

    [Test]
    public async Task ConvertImage_ConvertsToAllOtherFormats(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        await job.ConvertImageAsync(new RelativePath("photo.avif"), cancellationToken);

        // Assert
        await Assert.That(fileProvider.GetFileInfo("photo.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.jpg").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.png").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.thumb.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.thumb.jpg").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.thumb.png").Exists).IsTrue();
    }

    [Test]
    public async Task ConvertImage_CreatesSourceFormatThumbnail(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        await job.ConvertImageAsync(new RelativePath("photo.avif"), cancellationToken);

        // Assert
        await Assert.That(fileProvider.GetFileInfo("photo.avif").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.thumb.avif").Exists).IsTrue();
    }

    [Test]
    public async Task ConvertImage_ConvertsJpegSources(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("photo.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        await job.ConvertImageAsync(new RelativePath("photo.jpg"), cancellationToken);

        // Assert
        await Assert.That(fileProvider.GetFileInfo("photo.avif").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.png").Exists).IsTrue();
    }

    [Test]
    public async Task ConvertImage_SkipsExistingFiles(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        var expectedContent = imageFactory.CreateTestImage(format: MagickFormat.WebP);
        fileProvider.Write("photo.webp", expectedContent);

        // Act
        await job.ConvertImageAsync(new RelativePath("photo.avif"), cancellationToken);

        // Assert
        var existingContent = FileProviderExtensions.ReadAsBytes(fileProvider.GetFileInfo("photo.webp"));
        await Assert.That(existingContent).IsEquivalentTo(expectedContent);
    }

    [Test]
    [Arguments("photo.thumb.jpg", true)]
    [Arguments("photo.thumb.png", true)]
    [Arguments("photo.thumb.avif", true)]
    [Arguments("photo.avif", false)]
    [Arguments("photo.jpg", false)]
    [Arguments("thumb.jpg", false)]
    [Arguments("photo.txt", false)]
    public async Task IsThumbnailFile_ReturnsExpectedResult(string path, bool expected)
    {
        // Act
        var result = imageEnumerator.IsThumbnailFile(path);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("photo.avif", true)]
    [Arguments("photo.webp", true)]
    [Arguments("photo.jpg", true)]
    [Arguments("photo.png", true)]
    [Arguments("photo.bmp", false)]
    [Arguments("photo.tiff", false)]
    [Arguments("photo.txt", false)]
    [Arguments("photo", false)]
    public async Task IsImageFile_ReturnsExpectedResult(string path, bool expected)
    {
        // Act
        var result = imageEnumerator.IsImageFile(path);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ConvertImage_ThumbnailIsSmallerThanOriginal(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("large.avif", imageFactory.CreateTestImage(800, 600, MagickFormat.Avif));

        // Act
        await job.ConvertImageAsync(new RelativePath("large.avif"), cancellationToken);

        // Assert
        var thumbnailData = FileProviderExtensions.ReadAsBytes(fileProvider.GetFileInfo("large.thumb.webp"));
        var (thumbnailWidth, thumbnailHeight) = imageFactory.GetDimensions(thumbnailData);
        await Assert.That(thumbnailWidth).IsLessThanOrEqualTo(defaultConverterOptions.ThumbnailMaxWidth);
        await Assert.That(thumbnailHeight).IsLessThanOrEqualTo(defaultConverterOptions.ThumbnailMaxHeight);
    }

    [Test]
    public async Task ConvertImage_FullResolutionIsNotResized(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("large.avif", imageFactory.CreateTestImage(800, 600, MagickFormat.Avif));

        // Act
        await job.ConvertImageAsync(new RelativePath("large.avif"), cancellationToken);

        // Assert
        var fullData = FileProviderExtensions.ReadAsBytes(fileProvider.GetFileInfo("large.webp"));
        var (fullWidth, fullHeight) = imageFactory.GetDimensions(fullData);
        await Assert.That(fullWidth).IsEqualTo(800);
        await Assert.That(fullHeight).IsEqualTo(600);
    }

    [Test]
    public async Task WatchForChanges_ConvertsFilesAddedAfterEachConversion(CancellationToken testCancellation)
    {
        // Arrange: the file-provider watch token is one-shot. After the first
        // detected change consumes it, a later addition only triggers a fresh
        // scan if the job re-registers the watch after every scan.
        using var jobCancellation = new CancellationTokenSource();
        var sentinel = imageFactory.CreateTestImage(MagickFormat.Avif);
        var firstImage = imageFactory.CreateTestImage(MagickFormat.Jpeg);
        var secondImage = imageFactory.CreateTestImage(MagickFormat.Avif);

        try
        {
            // Act
            // The sentinel is converted by the initial scan; waiting for its
            // last output guarantees the initial scan is done and the watch is
            // about to be registered before any additions are made.
            fileProvider.AddFile("sentinel.avif", sentinel);
            await job.StartAsync(jobCancellation.Token);
            await WaitForFileAsync("sentinel.thumb.png", testCancellation);

            // The first addition can only be converted by a token-triggered scan,
            // which consumes the one-shot watch token.
            fileProvider.AddFile("first.jpg", firstImage);
            await WaitForConversionByRetouchingAsync("first.jpg", "first.avif", firstImage, testCancellation);

            // A second addition must also trigger a scan (only after the fix).
            fileProvider.AddFile("second.avif", secondImage);
            await WaitForFileAsync("second.webp", testCancellation);
        }
        finally
        {
            jobCancellation.Cancel();
            await job.StopAsync(testCancellation);
        }

        // Assert
        await Assert.That(fileProvider.GetFileInfo("second.webp").Exists).IsTrue();
    }

    private async Task WaitForConversionByRetouchingAsync(
        string inputPath, string outputPath, byte[] inputBytes, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            while (!fileProvider.GetFileInfo(outputPath).Exists)
            {
                // Re-assert the input so that, once the watch token is registered,
                // the matching change fires (the token is one-shot per write).
                fileProvider.Write(inputPath, inputBytes);
                await Task.Delay(50, linked.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for '{outputPath}' to be created; the file change watch did not fire.");
        }
    }

    private async Task WaitForFileAsync(string path, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            while (!fileProvider.GetFileInfo(path).Exists)
            {
                await Task.Delay(50, linked.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for '{path}' to be created; the file change watch did not fire again.");
        }
    }

    [Test]
    public Task ScanAndConvertAsync_EmptyDirectory_DoesNotThrow(CancellationToken cancellationToken) =>
        job.ScanAndConvertAsync(cancellationToken);

    [Test]
    public async Task ScanAndConvertAsync_ConvertsAllSourceImages(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.CreateDirectory("subdir");
        fileProvider.AddFile("subdir/other.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));

        // Act
        await job.ScanAndConvertAsync(cancellationToken);

        // Assert
        await Assert.That(fileProvider.GetFileInfo("photo.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.jpg").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.png").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("subdir/other.avif").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("subdir/other.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("subdir/other.png").Exists).IsTrue();
    }
}
