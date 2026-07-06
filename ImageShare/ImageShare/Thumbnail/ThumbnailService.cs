using ImageMagick;
using Microsoft.Extensions.Options;

namespace ImageShare.Thumbnail;

public sealed class ThumbnailService(IOptions<ThumbnailOptions> options) : IThumbnailService
{
    private readonly ThumbnailOptions _defaults = options.Value;

    public ReadOnlyMemory<byte> GenerateThumbnail(ReadOnlySpan<byte> imageData, ThumbnailOptions? options = null)
    {
        var opts = options ?? _defaults;

        using var image = new MagickImage(imageData);

        var geometry = new MagickGeometry((uint)opts.MaxWidth, (uint)opts.MaxHeight)
        {
            IgnoreAspectRatio = false,
        };

        image.Resize(geometry);
        image.Format = ParseFormat(opts.OutputFormat);
        image.Quality = opts.Quality;

        return image.ToByteArray();
    }

    private static MagickFormat ParseFormat(string format)
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
