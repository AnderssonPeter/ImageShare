namespace ImageShare.UsageAgreement;

public interface IUsageAgreement
{
    bool IsEnabled { get; }
    bool IsAccepted { get; }
    void EnsureAccepted();
    void Accept();
}
