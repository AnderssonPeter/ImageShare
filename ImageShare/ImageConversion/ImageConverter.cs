using ImageMagick;
using Microsoft.Extensions.Options;

namespace ImageShare.ImageConversion;

public sealed class ImageConverter(IOptions<ImageConverterOptions> options)
{
    private readonly ImageConverterOptions _options = options.Value;

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

        return image.ToByteArray();
    }

    public ReadOnlyMemory<byte> ConvertFull(ReadOnlySpan<byte> imageData, MagickFormat format) =>
        Convert(imageData, format, _options.FullQuality);

    public ReadOnlyMemory<byte> ConvertThumbnail(ReadOnlySpan<byte> imageData, MagickFormat format) =>
        Convert(imageData, format, _options.ThumbnailQuality, _options.ThumbnailMaxWidth, _options.ThumbnailMaxHeight);

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
