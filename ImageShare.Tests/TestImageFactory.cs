using ImageMagick;

namespace ImageShare.Tests;

public sealed class TestImageFactory
{
    public byte[] CreateTestImage(int width = 100, int height = 100, MagickFormat format = MagickFormat.Avif)
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, (uint)width, (uint)height);
        image.Format = format;
        return image.ToByteArray();
    }

    public byte[] CreateTestImage(MagickFormat format) => CreateTestImage(100, 100, format);

    public byte[] CreateThumbnail() => CreateThumbnail(MagickColors.DodgerBlue);

    public byte[] CreateThumbnail(IMagickColor<byte> color)
    {
        using var image = new MagickImage(color, 50, 50);
        image.Format = MagickFormat.Jpeg;
        return image.ToByteArray();
    }

    public (int Width, int Height) GetDimensions(ReadOnlyMemory<byte> data)
    {
        using var image = new MagickImage(data.Span);
        return ((int)image.Width, (int)image.Height);
    }

    public (int Width, int Height) GetDimensions(byte[] data)
    {
        using var image = new MagickImage(data);
        return ((int)image.Width, (int)image.Height);
    }

    public MagickFormat GetFormat(ReadOnlyMemory<byte> data)
    {
        using var image = new MagickImage(data.Span);
        return image.Format;
    }
}
