using ImageMagick;

namespace ImageShare.Thumbnail;

public sealed class ThumbnailService(ThumbnailOptions? defaults = null) : IThumbnailService
{
    private readonly ThumbnailOptions _defaults = defaults ?? new ThumbnailOptions();

    public ReadOnlyMemory<byte> GenerateThumbnail(ReadOnlySpan<byte> avifData, ThumbnailOptions? options = null)
    {
        var opts = options ?? _defaults;

        var settings = new MagickReadSettings
        {
            Format = MagickFormat.Avif,
        };

        using var image = new MagickImage(avifData, settings);

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
