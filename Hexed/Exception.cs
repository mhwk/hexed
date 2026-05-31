namespace Hexed;

public abstract class HexedException : System.Exception
{
    private HexedException(string? message) : base(message)
    {
    }

    private HexedException(string? message, System.Exception? inner) : base(message, inner)
    {
    }

    public sealed class CircularDependency(string? message) : HexedException(message);

    public sealed class ModuleAlreadyRegistered(string? message) : HexedException(message);

    public sealed class InvalidConfiguration(string? message) : HexedException(message);

    public sealed class ModuleActivation(string? message) : HexedException(message);

    public sealed class UnknownModule(string? message) : HexedException(message);

    public sealed class UnknownConfigureInvocation(string? message) : HexedException(message);

    public sealed class InvalidModuleDeclaration(string? message) : HexedException(message);
}
