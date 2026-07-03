namespace ImageShare.Thumbnail;

public interface IThumbnailService
{
    ReadOnlyMemory<byte> GenerateThumbnail(ReadOnlySpan<byte> avifData, ThumbnailOptions? options = null);
}
