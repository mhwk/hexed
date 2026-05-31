using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Hexed;

/// <summary>
/// Describes module dependency and configuration relationships.
/// </summary>
public interface Metadata
{
    /// <summary>
    /// Returns the types this module declares as Use&lt;T&gt; dependencies.
    /// </summary>
    IEnumerable<Type> UsedModules(Type moduleType);

    /// <summary>
    /// Returns the types this module declares as Glob&lt;T&gt; dependencies.
    /// </summary>
    IEnumerable<Type> GlobbedModules(Type moduleType);

    /// <summary>
    /// Returns the module types this module declares as Configure&lt;T&gt; dependencies.
    /// </summary>
    IEnumerable<Type> ConfiguredModules(Type moduleType);

    /// <summary>
    /// Returns the component types this module declares as Configure&lt;T&gt; dependencies.
    /// </summary>
    IEnumerable<Type> ConfiguredComponents(Type componentType);

    /// <summary>
    /// Creates a module instance by type.
    /// </summary>
    Module CreateModule(Type moduleType);

    /// <summary>
    /// Invokes Configure&lt;T&gt;.Configure(dependency) on the given module instance.
    /// </summary>
    void InvokeConfigure(object module, Type configurableType, object dependency);

    [RequiresDynamicCode("Use Hexed.Analyzer for AOT compatibility.")]
    [RequiresUnreferencedCode("Use Hexed.Analyzer for AOT compatibility.")]
    internal sealed class Reflection : Metadata
    {
        public IEnumerable<Type> UsedModules(Type moduleType)
            => GenericInterfaceArguments(moduleType, typeof(Use<>));

        public IEnumerable<Type> GlobbedModules(Type moduleType)
            => GenericInterfaceArguments(moduleType, typeof(Glob<>));

        public IEnumerable<Type> ConfiguredModules(Type moduleType)
            => GenericInterfaceArguments(moduleType, typeof(Configure<>))
                .Where(t => typeof(Module).IsAssignableFrom(t));

        public IEnumerable<Type> ConfiguredComponents(Type componentType)
            => GenericInterfaceArguments(componentType, typeof(Configure<>))
                .Where(t => !typeof(Module).IsAssignableFrom(t));

        public Module CreateModule(Type moduleType)
            => (Module?)Activator.CreateInstance(moduleType)
               ?? throw new HexedException.ModuleActivation($"Unable to activate module {moduleType}");

        public void InvokeConfigure(object module, Type configurableType, object dependency)
            => typeof(Configure<>)
                .MakeGenericType(configurableType)
                .GetMethod(
                    nameof(Configure<>.Configure),
                    BindingFlags.Instance | BindingFlags.Public)!
                .Invoke(module, [dependency]);

        private static IEnumerable<Type> GenericInterfaceArguments(Type type, Type openGeneric)
            => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric)
                .Select(i => i.GetGenericArguments()[0]);
    }
}