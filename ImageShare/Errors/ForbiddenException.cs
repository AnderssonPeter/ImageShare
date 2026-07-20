namespace ImageShare.Errors;

public sealed class ForbiddenException : ImageShareException
{
    public ForbiddenException() : base("Access is forbidden.") { }

    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
}
