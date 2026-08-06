using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace ImageShare.Logging;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class ReverseProxyOptions : IValidatableObject
{
    public bool Enabled { get; set; }

    public IList<string> KnownProxies { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var index = 0; index < KnownProxies.Count; index++)
        {
            var proxy = KnownProxies[index];
            if (string.IsNullOrWhiteSpace(proxy))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"KnownProxies[{index}] must not be empty."),
                    [nameof(KnownProxies)]);
            }
            else if (!IPAddress.TryParse(proxy, out _))
            {
                yield return new ValidationResult(
                    FormattableString.Invariant($"KnownProxies[{index}] '{proxy}' is not a valid IP address."),
                    [nameof(KnownProxies)]);
            }
        }
    }
}
