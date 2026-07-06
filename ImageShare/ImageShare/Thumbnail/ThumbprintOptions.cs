using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Thumbnail;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class ThumbprintOptions
{
    public const string ThumbInfix = ".thumb";

    [Required]
    public string ThumbSuffix { get; set; } = ThumbInfix;

    [Required]
    public string ThumbFormat { get; set; } = "jpeg";

    public bool WatchForChanges { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int MaxConcurrentGenerations { get; set; } = 4;
}
