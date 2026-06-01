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
public sealed record Metadata
{
    /// <summary>
    /// The types the module declares as Use&lt;T&gt; dependencies.
    /// </summary>
    public required Type[] UsedModules { get; init; }

    /// <summary>
    /// The types the module declares as Glob&lt;T&gt; dependencies.
    /// </summary>
    public required Type[] GlobbedModules { get; init; }

    /// <summary>
    /// The module types the module declares as Configure&lt;T&gt; dependencies.
    /// </summary>
    public required Type[] ConfiguredModules { get; init; }

    /// <summary>
    /// The component types the module declares as Configure&lt;T&gt; dependencies.
    /// </summary>
    public required Type[] ConfiguredComponents { get; init; }

    /// <summary>
    /// Create the module instance.
    /// </summary>
    public required Func<Module> Factory { get; init; }

    /// <summary>
    /// Invokes Configure&lt;T&gt;.Configure(dependency) on the given module instance.
    /// </summary>
    public required Action<Type, object> Configure { get; init; }

    public sealed class Registry
    {
        private readonly ConcurrentDictionary<string, Metadata> _byType = new();
        
        public void Register(Type type, Metadata metadata)
        {
        }
        
        public Metadata this[Type index]
        {
            get
            {
                
            }
        }
        
        /// <summary>
        /// Validates that the module's dependency declarations are consistent.
        /// Throws <see cref="HexedException.InvalidModuleDeclaration"/> on conflict.
        /// </summary>
        public void AssertValidModule(Type moduleType)
        {
            // static void ThrowConflict(Type mt, string kind, Type ct)
            // {
            //     throw new HexedException.InvalidModuleDeclaration(
            //         $"{mt.TypeName()} declares both {kind}<{ct.TypeName()}> and Configure<{ct.TypeName()}>, which are incompatible.");
            // }
            //
            // var globbed = GlobbedModules(moduleType);
            // var used = UsedModules(moduleType);
            //
            // foreach (var t in ConfiguredModules(moduleType))
            // {
            //     if (Array.IndexOf(globbed, t) >= 0)
            //     {
            //         ThrowConflict(moduleType, "Glob", t);
            //     }
            //
            //     if (Array.IndexOf(used, t) >= 0)
            //     {
            //         ThrowConflict(moduleType, "Use", t);
            //     }
            // }
            //
            // foreach (var t in ConfiguredComponents(moduleType))
            // {
            //     if (Array.IndexOf(globbed, t) >= 0)
            //     {
            //         ThrowConflict(moduleType, "Glob", t);
            //     }
            //
            //     if (Array.IndexOf(used, t) >= 0)
            //     {
            //         ThrowConflict(moduleType, "Use", t);
            //     }
            // }
        }
    }
}