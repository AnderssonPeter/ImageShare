using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Authentication;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class ApiKeySettings : IValidatableObject
{
    public IDictionary<string, ApiKeyEntry> Keys { get; set; } = new Dictionary<string, ApiKeyEntry>(StringComparer.Ordinal);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (name, entry) in Keys)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"API key '{name}' is missing a Key."),
                    [nameof(Keys)]);
            }

            if (string.IsNullOrWhiteSpace(entry.Filter))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"API key '{name}' is missing a Filter."),
                    [nameof(Keys)]);
            }
        }
    }
}
