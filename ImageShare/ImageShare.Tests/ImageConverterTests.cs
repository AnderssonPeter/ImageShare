using ImageMagick;
using ImageShare.ImageConversion;
using Microsoft.Extensions.Options;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageConverterTests(ImageConverter converter)
{
    private static readonly ImageConverterOptions DefaultOptions = new()
    {
        FullQuality = 80,
        ThumbnailQuality = 70,
        ThumbnailMaxWidth = 200,
        ThumbnailMaxHeight = 200,
    };

    private static readonly ImageConverter DefaultConverter = new(Options.Create(DefaultOptions));

    private static byte[] CreateTestImage(int width = 100, int height = 100)
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, (uint)width, (uint)height);
        image.Format = MagickFormat.Avif;
        return image.ToByteArray();
    }

    private static (int Width, int Height) Dimensions(ReadOnlyMemory<byte> data)
    {
        using var image = new MagickImage(data.Span);
        return ((int)image.Width, (int)image.Height);
    }

    private static MagickFormat GetFormat(ReadOnlyMemory<byte> data)
    {
        using var image = new MagickImage(data.Span);
        return image.Format;
    }

    [Test]
    [Arguments("jpeg", MagickFormat.Jpeg)]
    [Arguments("jpg", MagickFormat.Jpeg)]
    [Arguments("JPG", MagickFormat.Jpeg)]
    [Arguments("png", MagickFormat.Png)]
    [Arguments("webp", MagickFormat.WebP)]
    [Arguments("avif", MagickFormat.Avif)]
    public async Task ParseFormat_KnownFormat_ReturnsCorrectMagickFormat(string input, MagickFormat expected)
    {
        var result = ImageConverter.ParseFormat(input);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("bmp")]
    [Arguments("tiff")]
    [Arguments("unknown")]
    public async Task ParseFormat_UnknownFormat_FallsBackToJpeg(string input)
    {
        var result = ImageConverter.ParseFormat(input);

        await Assert.That(result).IsEqualTo(MagickFormat.Jpeg);
    }

    [Test]
    public async Task Convert_ChangesFormat()
    {
        var source = CreateTestImage();

        var result = converter.Convert(source, MagickFormat.Png, 80);

        await Assert.That(GetFormat(result)).IsEqualTo(MagickFormat.Png);
    }

    [Test]
    [Arguments(50u)]
    [Arguments(0u)]
    [Arguments(100u)]
    public async Task Convert_AppliesQuality(uint quality)
    {
        var source = CreateTestImage();

        var result = converter.Convert(source, MagickFormat.Jpeg, quality);

        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Convert_NoDimensions_DoesNotResize()
    {
        var source = CreateTestImage(800, 600);

        var result = converter.Convert(source, MagickFormat.Png, 80);

        var (width, height) = Dimensions(result);
        await Assert.That(width).IsEqualTo(800);
        await Assert.That(height).IsEqualTo(600);
    }

    [Test]
    public async Task Convert_WithWidth_ResizesAndPreservesAspectRatio()
    {
        var source = CreateTestImage(800, 600);

        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxWidth: 200);

        var (width, height) = Dimensions(result);
        await Assert.That(width).IsEqualTo(200);
        await Assert.That(height).IsEqualTo(150);
    }

    [Test]
    public async Task Convert_WithHeight_ResizesAndPreservesAspectRatio()
    {
        var source = CreateTestImage(800, 600);

        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxHeight: 150);

        var (width, height) = Dimensions(result);
        await Assert.That(width).IsEqualTo(200);
        await Assert.That(height).IsEqualTo(150);
    }

    [Test]
    public async Task Convert_WithWidthAndHeight_ConstrainsToBoth()
    {
        var source = CreateTestImage(800, 600);

        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxWidth: 100, maxHeight: 100);

        var (width, height) = Dimensions(result);
        await Assert.That(width).IsLessThanOrEqualTo(100);
        await Assert.That(height).IsLessThanOrEqualTo(100);
    }

    [Test]
    public async Task Convert_SmallImage_StillFitsWithinMaxDimensions()
    {
        var source = CreateTestImage(50, 50);

        var result = converter.Convert(source, MagickFormat.Jpeg, 80, maxWidth: 200, maxHeight: 200);

        var (width, height) = Dimensions(result);
        await Assert.That(width).IsLessThanOrEqualTo(200);
        await Assert.That(height).IsLessThanOrEqualTo(200);
        await Assert.That(width).IsGreaterThan(0);
        await Assert.That(height).IsGreaterThan(0);
    }

    [Test]
    public async Task Convert_ResultIsNonEmpty()
    {
        var source = CreateTestImage();

        var result = converter.Convert(source, MagickFormat.WebP, 80);

        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ConvertFull_UsesConfiguredFullQuality()
    {
        var source = CreateTestImage();
        var options = new ImageConverterOptions { FullQuality = 50, ThumbnailQuality = 90, ThumbnailMaxWidth = 200, ThumbnailMaxHeight = 200 };
        var customConverter = new ImageConverter(Options.Create(options));

        var result = customConverter.ConvertFull(source, MagickFormat.Jpeg);

        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ConvertFull_DoesNotResize()
    {
        var source = CreateTestImage(800, 600);

        var result = converter.ConvertFull(source, MagickFormat.Png);

        var (width, height) = Dimensions(result);
        await Assert.That(width).IsEqualTo(800);
        await Assert.That(height).IsEqualTo(600);
    }

    [Test]
    public async Task ConvertThumbnail_UsesConfiguredDimensions()
    {
        var source = CreateTestImage(800, 600);

        var result = converter.ConvertThumbnail(source, MagickFormat.Jpeg);

        var (width, height) = Dimensions(result);
        await Assert.That(width).IsLessThanOrEqualTo(200);
        await Assert.That(height).IsLessThanOrEqualTo(200);
    }

    [Test]
    public async Task ConvertThumbnail_UsesThumbnailQuality()
    {
        var source = CreateTestImage();
        var options = new ImageConverterOptions { FullQuality = 80, ThumbnailQuality = 30, ThumbnailMaxWidth = 200, ThumbnailMaxHeight = 200 };
        var customConverter = new ImageConverter(Options.Create(options));

        var result = customConverter.ConvertThumbnail(source, MagickFormat.Jpeg);

        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ConvertThumbnail_OutputFormatIsCorrect()
    {
        var source = CreateTestImage();

        var result = converter.ConvertThumbnail(source, MagickFormat.Avif);

        await Assert.That(GetFormat(result)).IsEqualTo(MagickFormat.Avif);
    }

    [Test]
    public async Task ConvertToAllFormats_EachFormatProducesValidImage()
    {
        var formats = new[] { MagickFormat.Jpeg, MagickFormat.Png, MagickFormat.WebP, MagickFormat.Avif };
        var source = CreateTestImage();

        foreach (var format in formats)
        {
            var result = converter.Convert(source, format, 80);

            using var image = new MagickImage(result.Span);
            await Assert.That((int)image.Width).IsGreaterThan(0);
            await Assert.That((int)image.Height).IsGreaterThan(0);
            await Assert.That(image.Format).IsEqualTo(format);
        }
    }
}