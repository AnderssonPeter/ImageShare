using ImageShare.Errors;
using ImageShare.UsageAgreement;

namespace ImageShare.Tests;

public sealed class TestUsageAgreement : IUsageAgreement
{
    public bool IsEnabled { get; set; }
    public bool IsAccepted { get; set; } = true;
    public bool WasAccepted { get; private set; }

    public void EnsureAccepted()
    {
        if (IsEnabled && !IsAccepted)
        {
            throw new UsageAgreementNotAcceptedException();
        }
    }

    public void Accept()
    {
        WasAccepted = true;
        IsAccepted = true;
    }
}
