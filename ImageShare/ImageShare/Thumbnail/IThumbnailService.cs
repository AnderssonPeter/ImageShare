namespace ImageShare.Thumbnail;

public interface IThumbnailService
{
    ReadOnlyMemory<byte> GenerateThumbnail(ReadOnlySpan<byte> imageData, ThumbnailOptions? options = null);
}
