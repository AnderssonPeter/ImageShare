using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.Authentication;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class ApiKeySettings : IValidatableObject
{
    public IList<ApiKeyEntry> Keys { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var index = 0; index < Keys.Count; index++)
        {
            var entry = Keys[index];
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"API key at index {index} is missing a Key."),
                    [nameof(Keys)]);
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"API key at index {index} is missing a Name."),
                    [nameof(Keys)]);
            }

            if (string.IsNullOrWhiteSpace(entry.Filter))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"API key at index {index} is missing a Filter."),
                    [nameof(Keys)]);
            }
        }
    }
}
