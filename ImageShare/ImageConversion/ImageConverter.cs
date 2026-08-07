using ImageMagick;
using Microsoft.Extensions.Options;

namespace ImageShare.ImageConversion;

public sealed class ImageConverter(IOptions<ImageConverterOptions> options)
{
    public ReadOnlyMemory<byte> Convert(ReadOnlySpan<byte> imageData, MagickFormat format, uint quality, int? maxWidth = null, int? maxHeight = null)
    {
        using var image = new MagickImage(imageData);

        image.Format = format;
        image.Quality = quality;

        if (maxWidth.HasValue || maxHeight.HasValue)
        {
            var geometry = new MagickGeometry
            {
                IgnoreAspectRatio = false,
            };

            if (maxWidth.HasValue)
            {
                geometry.Width = (uint)maxWidth.Value;
            }

            if (maxHeight.HasValue)
            {
                geometry.Height = (uint)maxHeight.Value;
            }

            image.Resize(geometry);
        }

        // AVIF uses AV1 4:2:0 chroma subsampling, which requires even dimensions.
        // An odd width or height makes the encoder pad to the next even row/column
        // with black pixels, producing a visible black line at the bottom/right edge.
        if (format == MagickFormat.Avif)
        {
            EnsureEvenDimensions(image);
        }

        return image.ToByteArray();
    }

    private static void EnsureEvenDimensions(MagickImage image)
    {
        var width = (int)image.Width;
        var height = (int)image.Height;
        var evenWidth = width & ~1;
        var evenHeight = height & ~1;

        if (evenWidth != width || evenHeight != height)
        {
            image.Crop(new MagickGeometry((uint)evenWidth, (uint)evenHeight));
        }
    }

    public ReadOnlyMemory<byte> ConvertFull(ReadOnlySpan<byte> imageData, MagickFormat format) =>
        Convert(imageData, format, options.Value.FullQuality);

    public ReadOnlyMemory<byte> ConvertThumbnail(ReadOnlySpan<byte> imageData, MagickFormat format) =>
        Convert(imageData, format, options.Value.ThumbnailQuality, options.Value.ThumbnailMaxWidth, options.Value.ThumbnailMaxHeight);

    public static MagickFormat ParseFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => MagickFormat.Jpeg,
            "png" => MagickFormat.Png,
            "webp" => MagickFormat.WebP,
            "avif" => MagickFormat.Avif,
            _ => MagickFormat.Jpeg,
        };
    }
}
