namespace ImageShare.Errors;

public sealed class NotAcceptableException : ImageShareException
{
    public NotAcceptableException() : base("None of the requested media types are supported.") { }

    public NotAcceptableException(string message) : base(message) { }

    public NotAcceptableException(string message, Exception innerException) : base(message, innerException) { }
}
