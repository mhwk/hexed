using Hexed.Text;
using System;
using System.Collections.Concurrent;

namespace Hexed;

public sealed record Metadata
{
    public required Type[] UsedModules { get; init; }

    public required Type[] GlobbedModules { get; init; }

    public required Type[] ConfiguredModules { get; init; }

    public required Type[] ConfiguredComponents { get; init; }

    public required Func<Module> Factory { get; init; }

    public required Action<object, Type, object> Configure { get; init; }

    public sealed class Registry
    {
        private readonly ConcurrentDictionary<Type, Metadata> _byType = new();

        public void Register(Type type, Metadata metadata)
        {
            _byType[type] = metadata;
        }

        public Metadata this[Type index]
        {
            get
            {
                if (_byType.TryGetValue(index, out var metadata))
                {
                    return metadata;
                }

                throw new HexedException.UnknownModule(
                    $"No metadata registered for {index.TypeName()}");
            }
        }

        public void AssertValidModule(Type moduleType)
        {
            var metadata = this[moduleType];
            var globbed = metadata.GlobbedModules;
            var used = metadata.UsedModules;

            foreach (var t in metadata.ConfiguredModules)
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

            foreach (var t in metadata.ConfiguredComponents)
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

        private static void ThrowConflict(Type moduleType, string kind, Type conflictType)
        {
            throw new HexedException.InvalidModuleDeclaration(
                $"{moduleType.TypeName()} declares both {kind}<{conflictType.TypeName()}> and Configure<{conflictType.TypeName()}>, which are incompatible.");
        }
    }
}