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
    public async Task ConvertImage_DoesNotCreateSourceFormat(CancellationToken cancellationToken)
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        await job.ConvertImageAsync(new RelativePath("photo.avif"), cancellationToken);

        // Assert
        await Assert.That(fileProvider.GetFileInfo("photo.thumb.avif").Exists).IsFalse();
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
