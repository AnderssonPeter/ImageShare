using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.DataProtection;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class DataProtectionOptions
{
    public const string DefaultKeyPath = "dataprotection-keys";

    [Required]
    public string KeyPath { get; set; } = DefaultKeyPath;
}
