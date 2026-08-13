using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Authentication;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class ApiKeySettings : Dictionary<string, ApiKeyEntry>, IValidatableObject
{
    public ApiKeySettings() : base(StringComparer.Ordinal) { }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (name, entry) in this)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"API key '{name}' is missing a Key."),
                    [name]);
            }

            if (string.IsNullOrWhiteSpace(entry.Filter))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"API key '{name}' is missing a Filter."),
                    [name]);
            }
        }
    }
}
