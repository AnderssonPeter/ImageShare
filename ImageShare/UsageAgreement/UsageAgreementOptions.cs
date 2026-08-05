using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.UsageAgreement;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class UsageAgreementOptions : IValidatableObject
{
    public IList<UsageAgreementText> Agreements { get; set; } = [];

    /// <summary>Agreement enforcement is active only when at least one agreement is configured.</summary>
    public bool IsEnabled => Agreements.Count > 0;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var index = 0; index < Agreements.Count; index++)
        {
            var entry = Agreements[index];
            if (string.IsNullOrWhiteSpace(entry.Language))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"Agreement at index {index} is missing a Language."),
                    [nameof(Agreements)]);
            }

            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"Agreement at index {index} is missing Text."),
                    [nameof(Agreements)]);
            }
        }
    }
}
