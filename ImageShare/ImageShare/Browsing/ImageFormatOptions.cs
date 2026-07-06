using System.ComponentModel.DataAnnotations;

namespace ImageShare.Browsing;

public sealed class ImageFormatOptions
{
    [Required, MinLength(1)]
    public required string[] SupportedFormats { get; set; }
}
