namespace ImageShare.Thumbnail;

public sealed class ThumbnailOptions
{
    public int MaxWidth { get; set; } = 200;
    public int MaxHeight { get; set; } = 200;
    public uint Quality { get; set; } = 80;
    public string OutputFormat { get; set; } = "jpeg";
}
