namespace Hexed;

public abstract class Exception : System.Exception
{
    private Exception(string? message) : base(message)
    {
    }

    private Exception(string? message, System.Exception? inner) : base(message, inner)
    {
    }

    public sealed class CircularDependency(string? message) : Exception(message);

    public sealed class ModuleAlreadyRegistered(string? message) : Exception(message);

    public sealed class InvalidConfiguration(string? message) : Exception(message);

    public sealed class ModuleActivation(string? message) : Exception(message);

    public sealed class UnknownModule(string? message) : Exception(message);

    public sealed class UnknownConfigureInvocation(string? message) : Exception(message);
}
