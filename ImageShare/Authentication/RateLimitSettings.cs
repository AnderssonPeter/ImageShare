using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Authentication;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class RateLimitSettings : IValidatableObject
{
    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PermitLimit <= 0)
        {
            yield return new ValidationResult(
                "The PermitLimit must be greater than zero.",
                [nameof(PermitLimit)]);
        }

        if (WindowSeconds <= 0)
        {
            yield return new ValidationResult(
                "The WindowSeconds must be greater than zero.",
                [nameof(WindowSeconds)]);
        }
    }
}
