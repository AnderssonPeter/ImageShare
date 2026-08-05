using System.Diagnostics.CodeAnalysis;

namespace ImageShare.UsageAgreement;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class UsageAgreementText
{
    public string Language { get; set; } = "";

    public string Text { get; set; } = "";
}
