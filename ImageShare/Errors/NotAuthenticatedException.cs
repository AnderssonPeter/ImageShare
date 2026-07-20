namespace ImageShare.Errors;

public sealed class NotAuthenticatedException : ImageShareException
{
    public NotAuthenticatedException() : base("Authentication is required to access this resource.") { }

    public NotAuthenticatedException(string message) : base(message) { }

    public NotAuthenticatedException(string message, Exception innerException) : base(message, innerException) { }
}
