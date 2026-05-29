namespace Hexed;

public sealed class Exception : System.Exception
{
    public Exception(string? message) : base(message)
    {
    }

    public Exception(string? message, System.Exception? innerException) : base(message, innerException)
    {
    }
}