using ImageMagick;
using ImageShare.Thumbnail;
using Microsoft.Extensions.Options;

namespace ImageShare.Tests;

public class ThumbnailServiceTests
{
    private static readonly ThumbnailService DefaultService = new(Options.Create(new ThumbnailOptions()));

    private static byte[] CreateTestAvif()
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, 800, 600);
        image.Format = MagickFormat.Avif;
        return image.ToByteArray();
    }

    private static int W(MagickImage img) => (int)img.Width;
    private static int H(MagickImage img) => (int)img.Height;

    [Test]
    public async Task GenerateThumbnail_DefaultOptions_ProducesJpegWithinMaxDimensions()
    {
        var avif = CreateTestAvif();
        var result = DefaultService.GenerateThumbnail(avif);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(W(thumb)).IsLessThanOrEqualTo(200);
        await Assert.That(H(thumb)).IsLessThanOrEqualTo(200);
        await Assert.That(thumb.Format).IsEqualTo(MagickFormat.Jpeg);
    }

    [Test]
    public async Task GenerateThumbnail_CustomWidth_RespectsMaxWidth()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { MaxWidth = 100, MaxHeight = 400 };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(W(thumb)).IsLessThanOrEqualTo(100);
        await Assert.That(H(thumb)).IsLessThanOrEqualTo(600);
    }

    [Test]
    public async Task GenerateThumbnail_CustomHeight_RespectsMaxHeight()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { MaxWidth = 400, MaxHeight = 100 };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(W(thumb)).IsLessThanOrEqualTo(800);
        await Assert.That(H(thumb)).IsLessThanOrEqualTo(100);
    }

    [Test]
    public async Task GenerateThumbnail_SmallSource_StillFitsWithinMaxDimensions()
    {
        using var src = new MagickImage(MagickColors.Red, 50, 50);
        src.Format = MagickFormat.Avif;
        var avif = src.ToByteArray();

        var result = DefaultService.GenerateThumbnail(avif);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(W(thumb)).IsLessThanOrEqualTo(200);
        await Assert.That(H(thumb)).IsLessThanOrEqualTo(200);
        await Assert.That(W(thumb)).IsGreaterThan(0);
        await Assert.That(H(thumb)).IsGreaterThan(0);
    }

    [Test]
    public async Task GenerateThumbnail_PngOutput_ProducesPng()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { OutputFormat = "png" };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(thumb.Format).IsEqualTo(MagickFormat.Png);
    }

    [Test]
    public async Task GenerateThumbnail_JpgOutput_ProducesJpeg()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { OutputFormat = "jpg" };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(thumb.Format).IsEqualTo(MagickFormat.Jpeg);
    }

    [Test]
    public async Task GenerateThumbnail_WebpOutput_ProducesWebp()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { OutputFormat = "webp" };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(thumb.Format).IsEqualTo(MagickFormat.WebP);
    }

    [Test]
    public async Task GenerateThumbnail_AvifOutput_ProducesAvif()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { OutputFormat = "avif" };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(thumb.Format).IsEqualTo(MagickFormat.Avif);
    }

    [Test]
    public async Task GenerateThumbnail_UnknownFormat_FallsBackToJpeg()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { OutputFormat = "bmp" };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(thumb.Format).IsEqualTo(MagickFormat.Jpeg);
    }

    [Test]
    public async Task GenerateThumbnail_CustomQuality_ProducesValidImage()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { Quality = 50 };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(W(thumb)).IsGreaterThan(0);
        await Assert.That(H(thumb)).IsGreaterThan(0);
    }

    [Test]
    public async Task GenerateThumbnail_ServiceWithDefaults_UsesProvidedDefaults()
    {
        var defaults = new ThumbnailOptions { MaxWidth = 50, MaxHeight = 50 };
        var service = new ThumbnailService(Options.Create(defaults));
        var avif = CreateTestAvif();

        var result = service.GenerateThumbnail(avif);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(W(thumb)).IsLessThanOrEqualTo(50);
        await Assert.That(H(thumb)).IsLessThanOrEqualTo(50);
    }

    [Test]
    public async Task GenerateThumbnail_NullOptions_FallsBackToDefaults()
    {
        var defaults = new ThumbnailOptions { OutputFormat = "png" };
        var service = new ThumbnailService(Options.Create(defaults));
        var avif = CreateTestAvif();

        var result = service.GenerateThumbnail(avif, null);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(thumb.Format).IsEqualTo(MagickFormat.Png);
    }

    [Test]
    public async Task GenerateThumbnail_PreservesAspectRatio()
    {
        var avif = CreateTestAvif();
        var options = new ThumbnailOptions { MaxWidth = 160, MaxHeight = 200 };

        var result = DefaultService.GenerateThumbnail(avif, options);

        using var thumb = new MagickImage(result.Span);
        await Assert.That(W(thumb)).IsEqualTo(160);
        await Assert.That(H(thumb)).IsEqualTo(120);
    }

    [Test]
    public async Task GenerateThumbnail_ResultIsNonEmpty()
    {
        var avif = CreateTestAvif();
        var result = DefaultService.GenerateThumbnail(avif);

        await Assert.That(result.Length).IsGreaterThan(0);
    }
}
