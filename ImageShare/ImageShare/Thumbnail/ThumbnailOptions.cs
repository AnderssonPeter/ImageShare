using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Thumbnail;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class ThumbnailOptions
{
    [Range(1, int.MaxValue)]
    public int MaxWidth { get; set; } = 200;

    [Range(1, int.MaxValue)]
    public int MaxHeight { get; set; } = 200;

    [Range(0, 100)]
    public uint Quality { get; set; } = 80;

    [Required]
    public string OutputFormat { get; set; } = "jpeg";
}
