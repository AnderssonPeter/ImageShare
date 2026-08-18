using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ImageShare.DataProtection;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class DataProtectionOptions
{
    public const string DefaultKeyStoragePath = "dataprotection-keys";

    [Required]
    public string KeyStoragePath { get; set; } = DefaultKeyStoragePath;
}
