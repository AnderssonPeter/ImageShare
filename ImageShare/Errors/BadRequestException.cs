namespace ImageShare.Errors;

public sealed class BadRequestException : ImageShareException
{
    public BadRequestException() : base("The request was invalid.") { }

    public BadRequestException(string message) : base(message) { }

    public BadRequestException(string message, Exception innerException) : base(message, innerException) { }
}
