using ImageMagick;
using ImageShare.ImageConversion;
using Microsoft.Extensions.Options;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageConverterTests(ImageConverter converter, TestImageFactory imageFactory)
{
    private static readonly ImageConverterOptions defaultOptions = new()
    {
        FullQuality = 80,
        ThumbnailQuality = 70,
        ThumbnailMaxWidth = 200,
        ThumbnailMaxHeight = 200,
    };

    private static readonly ImageConverter defaultConverter = new(Options.Create(defaultOptions));

    [Test]
    [Arguments("jpeg", MagickFormat.Jpeg)]
    [Arguments("jpg", MagickFormat.Jpeg)]
    [Arguments("JPG", MagickFormat.Jpeg)]
    [Arguments("png", MagickFormat.Png)]
    [Arguments("webp", MagickFormat.WebP)]
    [Arguments("avif", MagickFormat.Avif)]
    public async Task ParseFormat_KnownFormat_ReturnsCorrectMagickFormat(string input, MagickFormat expected)
    {
        // Act
        var result = ImageConverter.ParseFormat(input);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("bmp")]
    [Arguments("tiff")]
    [Arguments("unknown")]
    public async Task ParseFormat_UnknownFormat_FallsBackToJpeg(string input)
    {
        // Act
        var result = ImageConverter.ParseFormat(input);

        // Assert
        await Assert.That(result).IsEqualTo(MagickFormat.Jpeg);
    }

    [Test]
    public async Task Convert_ChangesFormat()
    {
        // Arrange
        var source = imageFactory.CreateTestImage();

        // Act
        var result = converter.Convert(source, MagickFormat.Png, 80);

        // Assert
        await Assert.That(imageFactory.GetFormat(result)).IsEqualTo(MagickFormat.Png);
    }

    [Test]
    [Arguments(50u)]
    [Arguments(0u)]
    [Arguments(100u)]
    public async Task Convert_AppliesQuality(uint quality)
    {
        // Arrange
        var source = imageFactory.CreateTestImage();

        // Act
        var result = converter.Convert(source, MagickFormat.Jpeg, quality);

        // Assert
        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Convert_NoDimensions_DoesNotResize()
    {
        // Arrange
        var source = imageFactory.CreateTestImage(800, 600);

        // Act
        var result = converter.Convert(source, MagickFormat.Png, 80);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width).IsEqualTo(800);
        await Assert.That(height).IsEqualTo(600);
    }

    [Test]
    public async Task Convert_WithWidth_ResizesAndPreservesAspectRatio()
    {
        // Arrange
        var source = imageFactory.CreateTestImage(800, 600);

        // Act
        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxWidth: 200);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width).IsEqualTo(200);
        await Assert.That(height).IsEqualTo(150);
    }

    [Test]
    public async Task Convert_WithHeight_ResizesAndPreservesAspectRatio()
    {
        // Arrange
        var source = imageFactory.CreateTestImage(800, 600);

        // Act
        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxHeight: 150);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width).IsEqualTo(200);
        await Assert.That(height).IsEqualTo(150);
    }

    [Test]
    public async Task Convert_WithWidthAndHeight_ConstrainsToBoth()
    {
        // Arrange
        var source = imageFactory.CreateTestImage(800, 600);

        // Act
        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxWidth: 100, maxHeight: 100);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width).IsLessThanOrEqualTo(100);
        await Assert.That(height).IsLessThanOrEqualTo(100);
    }

    [Test]
    public async Task Convert_SmallImage_StillFitsWithinMaxDimensions()
    {
        // Arrange
        var source = imageFactory.CreateTestImage(50, 50);

        // Act
        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxWidth: 200, maxHeight: 200);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width).IsLessThanOrEqualTo(200);
        await Assert.That(height).IsLessThanOrEqualTo(200);
        await Assert.That(width).IsGreaterThan(0);
        await Assert.That(height).IsGreaterThan(0);
    }

    [Test]
    public async Task Convert_ResultIsNonEmpty()
    {
        // Arrange
        var source = imageFactory.CreateTestImage();

        // Act
        var result = converter.Convert(source, MagickFormat.WebP, 80);

        // Assert
        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ConvertFull_UsesConfiguredFullQuality()
    {
        // Arrange
        var source = imageFactory.CreateTestImage();
        var options = new ImageConverterOptions { FullQuality = 50, ThumbnailQuality = 90, ThumbnailMaxWidth = 200, ThumbnailMaxHeight = 200 };
        var customConverter = new ImageConverter(Options.Create(options));

        // Act
        var result = customConverter.ConvertFull(source, MagickFormat.Jpeg);

        // Assert
        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ConvertFull_DoesNotResize()
    {
        // Arrange
        var source = imageFactory.CreateTestImage(800, 600);

        // Act
        var result = converter.ConvertFull(source, MagickFormat.Png);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width).IsEqualTo(800);
        await Assert.That(height).IsEqualTo(600);
    }

    [Test]
    public async Task ConvertThumbnail_UsesConfiguredDimensions()
    {
        // Arrange
        var source = imageFactory.CreateTestImage(800, 600);

        // Act
        var result = converter.ConvertThumbnail(source, MagickFormat.Jpeg);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width).IsLessThanOrEqualTo(200);
        await Assert.That(height).IsLessThanOrEqualTo(200);
    }

    [Test]
    public async Task ConvertThumbnail_UsesThumbnailQuality()
    {
        // Arrange
        var source = imageFactory.CreateTestImage();
        var options = new ImageConverterOptions { FullQuality = 80, ThumbnailQuality = 30, ThumbnailMaxWidth = 200, ThumbnailMaxHeight = 200 };
        var customConverter = new ImageConverter(Options.Create(options));

        // Act
        var result = customConverter.ConvertThumbnail(source, MagickFormat.Jpeg);

        // Assert
        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ConvertThumbnail_OutputFormatIsCorrect()
    {
        // Arrange
        var source = imageFactory.CreateTestImage();

        // Act
        var result = converter.ConvertThumbnail(source, MagickFormat.Avif);

        // Assert
        await Assert.That(imageFactory.GetFormat(result)).IsEqualTo(MagickFormat.Avif);
    }

    [Test]
    public async Task ConvertThumbnail_Avif_AlwaysProducesEvenDimensions()
    {
        // Arrange — 800x533 resized into 200x200 yields 200x133 (odd height),
        // which would make the AV1 encoder pad a black bottom row.
        var source = imageFactory.CreateTestImage(800, 533);

        // Act
        var result = converter.ConvertThumbnail(source, MagickFormat.Avif);

        // Assert
        var (width, height) = imageFactory.GetDimensions(result);
        await Assert.That(width % 2).IsEqualTo(0);
        await Assert.That(height % 2).IsEqualTo(0);
    }

    [Test]
    public async Task ConvertToAllFormats_EachFormatProducesValidImage()
    {
        // Arrange
        var formats = new[] { MagickFormat.Jpeg, MagickFormat.Png, MagickFormat.WebP, MagickFormat.Avif };
        var source = imageFactory.CreateTestImage(width: 64, height: 32);

        // Act
        foreach (var format in formats)
        {
            var result = converter.Convert(source, format, 80);

            using var image = new MagickImage(result.Span);

            // Assert
            await Assert.That((int)image.Width).IsEqualTo(64);
            await Assert.That((int)image.Height).IsEqualTo(32);
            await Assert.That(image.Format).IsEqualTo(format);
        }
    }
}
