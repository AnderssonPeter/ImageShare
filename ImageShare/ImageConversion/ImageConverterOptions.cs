using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.ImageConversion;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class ImageConverterOptions
{
    public const string ThumbnailInfix = ".thumb";

    [Range(0, 100)]
    public uint FullQuality { get; set; } = 95;

    [Range(0, 100)]
    public uint ThumbnailQuality { get; set; } = 85;

    [Range(1, int.MaxValue)]
    public int ThumbnailMaxWidth { get; set; } = 200;

    [Range(1, int.MaxValue)]
    public int ThumbnailMaxHeight { get; set; } = 200;
}
