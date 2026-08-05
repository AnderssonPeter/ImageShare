namespace ImageShare.Errors;

public sealed class UsageAgreementNotAcceptedException : ImageShareException
{
    public UsageAgreementNotAcceptedException() : base("The usage agreement must be accepted before this resource can be accessed.") { }

    public UsageAgreementNotAcceptedException(string message) : base(message) { }

    public UsageAgreementNotAcceptedException(string message, Exception innerException) : base(message, innerException) { }
}
