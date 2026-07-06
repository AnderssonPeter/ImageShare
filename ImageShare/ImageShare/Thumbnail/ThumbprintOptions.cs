namespace ImageShare.Thumbnail;

public class ThumbprintOptions
{
    public const string ThumbInfix = ".thumb";

    public string[] ImageExtensions { get; set; } = [".avif", ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".tif"];
    public string ThumbSuffix { get; set; } = ThumbInfix;
    public string ThumbFormat { get; set; } = "jpeg";
    public bool WatchForChanges { get; set; } = true;
    public int MaxConcurrentGenerations { get; set; } = 4;
}
