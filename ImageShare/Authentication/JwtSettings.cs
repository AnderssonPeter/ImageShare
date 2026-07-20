using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Authentication;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class JwtSettings : IValidatableObject
{
    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    [Required]
    public string SigningKey { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SigningKey.Length < 32)
        {
            yield return new ValidationResult(
                "The SigningKey must be at least 32 characters long.",
                [nameof(SigningKey)]);
        }
    }
}
