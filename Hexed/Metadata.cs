using Hexed.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Hexed;

/// <summary>
/// Describes module dependency and configuration relationships.
/// </summary>
public interface Metadata
{
    /// <summary>
    /// Returns the types this module declares as Use&lt;T&gt; dependencies.
    /// </summary>
    Type[] UsedModules(Type moduleType);

    /// <summary>
    /// Returns the types this module declares as Glob&lt;T&gt; dependencies.
    /// </summary>
    Type[] GlobbedModules(Type moduleType);

    /// <summary>
    /// Returns the module types this module declares as Configure&lt;T&gt; dependencies.
    /// </summary>
    Type[] ConfiguredModules(Type moduleType);

    /// <summary>
    /// Returns the component types this module declares as Configure&lt;T&gt; dependencies.
    /// </summary>
    Type[] ConfiguredComponents(Type componentType);

    /// <summary>
    /// Creates a module instance by type.
    /// </summary>
    Module CreateModule(Type moduleType);

    /// <summary>
    /// Invokes Configure&lt;T&gt;.Configure(dependency) on the given module instance.
    /// </summary>
    void InvokeConfigure(object module, Type configurableType, object dependency);

    /// <summary>
    /// Validates that the module's dependency declarations are consistent.
    /// Throws <see cref="HexedException.InvalidModuleDeclaration"/> on conflict.
    /// </summary>
    void AssertValidModule(Type moduleType)
    {
        static void ThrowConflict(Type mt, string kind, Type ct)
        {
            throw new HexedException.InvalidModuleDeclaration(
                $"{mt.TypeName()} declares both {kind}<{ct.TypeName()}> and Configure<{ct.TypeName()}>, which are incompatible.");
        }

        var globbed = GlobbedModules(moduleType);
        var used = UsedModules(moduleType);

        foreach (var t in ConfiguredModules(moduleType))
        {
            if (Array.IndexOf(globbed, t) >= 0)
            {
                ThrowConflict(moduleType, "Glob", t);
            }

            if (Array.IndexOf(used, t) >= 0)
            {
                ThrowConflict(moduleType, "Use", t);
            }
        }

        foreach (var t in ConfiguredComponents(moduleType))
        {
            if (Array.IndexOf(globbed, t) >= 0)
            {
                ThrowConflict(moduleType, "Glob", t);
            }

            if (Array.IndexOf(used, t) >= 0)
            {
                ThrowConflict(moduleType, "Use", t);
            }
        }
    }

    [RequiresDynamicCode("Use Hexed.Analyzer for AOT compatibility.")]
    [RequiresUnreferencedCode("Use Hexed.Analyzer for AOT compatibility.")]
    internal sealed class Reflection : Metadata
    {
        private readonly ConcurrentDictionary<Type, Type[]> _usedModules = new();
        
        private readonly ConcurrentDictionary<Type, Type[]> _globbedModules = new();
        
        private readonly ConcurrentDictionary<Type, Type[]> _configuredModules = new();
        
        private readonly ConcurrentDictionary<Type, Type[]> _configuredComponents = new();

        public Type[] UsedModules(Type moduleType)
            => _usedModules.GetOrAdd(moduleType, static t
                => ExtractArguments(t, typeof(Use<>)));

        public Type[] GlobbedModules(Type moduleType)
            => _globbedModules.GetOrAdd(moduleType, static t
                => ExtractArguments(t, typeof(Glob<>)));

        public Type[] ConfiguredModules(Type moduleType)
            => _configuredModules.GetOrAdd(moduleType, static t
                => ExtractArguments(t, typeof(Configure<>)).Where(IsModule).ToArray());

        public Type[] ConfiguredComponents(Type componentType)
            => _configuredComponents.GetOrAdd(componentType, static t
                => ExtractArguments(t, typeof(Configure<>)).Where(x
                    => !IsModule(x)).ToArray());

        public Module CreateModule(Type moduleType)
            => (Module?)Activator.CreateInstance(moduleType)
               ?? throw new HexedException.ModuleActivation($"Unable to activate module {moduleType}");

        public void InvokeConfigure(object module, Type configurableType, object dependency)
        {
            var method = typeof(Configure<>)
                .MakeGenericType(configurableType)
                .GetMethod(
                    nameof(Configure<>.Configure),
                    BindingFlags.Instance | BindingFlags.Public);

            if (method is null)
            {
                throw new HexedException.UnknownConfigureInvocation(
                    $"Module {module.GetType()} does not implement Configure<{configurableType}> via a public instance method.");
            }

            try
            {
                method.Invoke(module, [dependency]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static bool IsModule(Type t) => typeof(Module).IsAssignableFrom(t);

        private static Type[] ExtractArguments(Type type, Type openGeneric)
            => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric)
                .Select(i => i.GetGenericArguments()[0])
                .ToArray();
    }
}