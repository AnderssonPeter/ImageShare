using ImageMagick;
using ImageShare.Browsing;
using ImageShare.ImageConversion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageConverterJobTests(ISyncWritableFileProvider fileProvider, ImageConverter converter, IOptions<ImageFormatOptions> imageFormats, ILoggerFactory loggerFactory)
{
    private readonly ImageConverterJob _job = new(
            fileProvider,
            converter,
            imageFormats,
            loggerFactory.CreateLogger<ImageConverterJob>());

    private static byte[] CreateTestImage(int width = 100, int height = 100, MagickFormat format = MagickFormat.Avif)
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, (uint)width, (uint)height);
        image.Format = format;
        return image.ToByteArray();
    }

    private void AddFile(string path, MagickFormat format = MagickFormat.Avif, int width = 100, int height = 100)
    {
        fileProvider.Create(Path.GetDirectoryName(path) ?? "");
        fileProvider.Write(path, CreateTestImage(width, height, format));
    }

    private static readonly ImageConverterOptions DefaultConverterOptions = new()
    {
        FullQuality = 80,
        ThumbnailQuality = 70,
        ThumbnailMaxWidth = 200,
        ThumbnailMaxHeight = 200,
    };

    private static (int Width, int Height) Dimensions(byte[] data)
    {
        using var image = new MagickImage(data);
        return ((int)image.Width, (int)image.Height);
    }

    [Test]
    public async Task ConvertImage_ConvertsToAllOtherFormats(CancellationToken cancellationToken)
    {
        AddFile("photo.avif");

        await _job.ConvertImageAsync("photo.avif", cancellationToken);

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
        AddFile("photo.avif");

        await _job.ConvertImageAsync("photo.avif", cancellationToken);

        await Assert.That(fileProvider.GetFileInfo("photo.thumb.avif").Exists).IsFalse();
    }

    [Test]
    public async Task ConvertImage_ConvertsJpegSources(CancellationToken cancellationToken)
    {
        AddFile("photo.jpg", MagickFormat.Jpeg);

        await _job.ConvertImageAsync("photo.jpg", cancellationToken);

        await Assert.That(fileProvider.GetFileInfo("photo.avif").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.png").Exists).IsTrue();
    }

    [Test]
    public async Task ConvertImage_SkipsExistingFiles(CancellationToken cancellationToken)
    {
        AddFile("photo.avif");
        var expectedContent = CreateTestImage(format: MagickFormat.WebP);
        fileProvider.Write("photo.webp", expectedContent);

        await _job.ConvertImageAsync("photo.avif", cancellationToken);

        var existingContent = FileProviderExtensions.ReadAsBytes(fileProvider.GetFileInfo("photo.webp"));
        await Assert.That(existingContent).IsEquivalentTo(expectedContent);
    }

    [Test]
    public async Task IsThumbprintFile_ReturnsTrueForThumbFiles()
    {
        var result = ImageConverterJob.IsThumbprintFile("photo.thumb.jpg");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsThumbprintFile_ReturnsFalseForNormalFiles()
    {
        var result = ImageConverterJob.IsThumbprintFile("photo.avif");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsImageFile_ReturnsTrueForSupportedFormats()
    {
        var result = _job.IsImageFile("photo.avif");

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsImageFile_ReturnsFalseForUnsupportedFormats()
    {
        var result = _job.IsImageFile("photo.bmp");

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ConvertImage_ThumbnailIsSmallerThanOriginal(CancellationToken cancellationToken)
    {
        AddFile("large.avif", width: 800, height: 600);

        await _job.ConvertImageAsync("large.avif", cancellationToken);

        var thumbnailData = FileProviderExtensions.ReadAsBytes(fileProvider.GetFileInfo("large.thumb.webp"));
        var (thumbnailWidth, thumbnailHeight) = Dimensions(thumbnailData);
        await Assert.That(thumbnailWidth).IsLessThanOrEqualTo(DefaultConverterOptions.ThumbnailMaxWidth);
        await Assert.That(thumbnailHeight).IsLessThanOrEqualTo(DefaultConverterOptions.ThumbnailMaxHeight);
    }

    [Test]
    public async Task ConvertImage_FullResolutionIsNotResized(CancellationToken cancellationToken)
    {
        AddFile("large.avif", width: 800, height: 600);

        await _job.ConvertImageAsync("large.avif", cancellationToken);

        var fullData = FileProviderExtensions.ReadAsBytes(fileProvider.GetFileInfo("large.webp"));
        var (fullWidth, fullHeight) = Dimensions(fullData);
        await Assert.That(fullWidth).IsEqualTo(800);
        await Assert.That(fullHeight).IsEqualTo(600);
    }

    [Test]
    public Task ScanAndConvertAsync_EmptyDirectory_DoesNotThrow(CancellationToken cancellationToken) =>
        _job.ScanAndConvertAsync(cancellationToken);

    [Test]
    public async Task ScanAndConvertAsync_ConvertsAllSourceImages(CancellationToken cancellationToken)
    {
        AddFile("photo.avif");
        AddFile("subdir/other.jpg", MagickFormat.Jpeg);

        await _job.ScanAndConvertAsync(cancellationToken);

        await Assert.That(fileProvider.GetFileInfo("photo.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.jpg").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("photo.png").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("subdir/other.avif").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("subdir/other.webp").Exists).IsTrue();
        await Assert.That(fileProvider.GetFileInfo("subdir/other.png").Exists).IsTrue();
    }
}
