using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Browsing;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class ImageFormatOptions : IValidatableObject
{
    [Required]
    public required string[] SupportedFormats { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SupportedFormats.Length == 0)
        {
            yield return new ValidationResult("At least one supported image format is required.");
        }

        foreach (var format in SupportedFormats)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                yield return new ValidationResult("Supported image format must not be empty.");
            }
        }
    }
}
