namespace ImageShare.Errors;

public abstract class ImageShareException : Exception
{
    protected ImageShareException() { }

    protected ImageShareException(string message) : base(message) { }

    protected ImageShareException(string message, Exception innerException) : base(message, innerException) { }
}
