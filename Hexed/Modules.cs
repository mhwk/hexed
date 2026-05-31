using Hexed.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hexed;

public sealed class Modules : IReadOnlyCollection<Module>
{
    public static Metadata Metadata
    {
        get
        {
#pragma warning disable IL2026, IL3050
            return field ??= new Metadata.Reflection();
#pragma warning restore IL2026, IL3050
        }
        set;
    }

    private readonly Glob _glob = new();

    private readonly Dictionary<Type, Module> _byType = new();

    private readonly List<Module> _sorted = new();

    private readonly HashSet<Type> _resolving = new();

    private Module Load(Type moduleType)
    {
        if (_resolving.Contains(moduleType))
        {
            throw new HexedException.CircularDependency(
                $"Circular dependency detected involving {moduleType.TypeName()}");
        }

        return _byType.TryGetValue(moduleType, out var existing)
            ? existing
            : Load(Metadata.CreateModule(moduleType));
    }

    public TModule Load<TModule>() where TModule : Module, new()
        => (TModule)Load(typeof(TModule));

    public TModule Load<TModule>(TModule module) where TModule : Module
    {
        var moduleType = module.GetType();

        if (_byType.TryGetValue(moduleType, out _))
        {
            throw new HexedException.ModuleAlreadyRegistered(
                $"Attempted to register {moduleType.TypeName()} via Load(instance) after it was already loaded. Register the instance higher up in the dependency tree, before modules that depend on it are loaded.");
        }

        _resolving.Add(moduleType);
        
        try
        {
            var globbedModules = Metadata.GlobbedModules(moduleType);
            var usedModules = Metadata.UsedModules(moduleType);
            var configuredTypes = Metadata.ConfiguredModules(moduleType)
                .Concat(Metadata.ConfiguredComponents(moduleType))
                .ToArray();

            var globConfigureConflict = globbedModules.Intersect(configuredTypes).ToArray();

            if (globConfigureConflict.Length > 0)
            {
                throw new HexedException.InvalidModuleDeclaration(
                    $"{moduleType.TypeName()} declares both Glob<{globConfigureConflict[0].TypeName()}> and Configure<{globConfigureConflict[0].TypeName()}>, which are incompatible.");
            }

            var useConfigureConflict = usedModules.Intersect(configuredTypes).ToArray();

            if (useConfigureConflict.Length > 0)
            {
                throw new HexedException.InvalidModuleDeclaration(
                    $"{moduleType.TypeName()} declares both Use<{useConfigureConflict[0].TypeName()}> and Configure<{useConfigureConflict[0].TypeName()}>, which are incompatible.");
            }

            foreach (var usedType in usedModules)
            {
                if (globbedModules.Contains(usedType) && !_glob.IsMatch(usedType))
                {
                    continue;
                }

                Load(usedType);
            }

            foreach (var configuredType in Metadata.ConfiguredModules(moduleType))
            {
                var dependency = Load(configuredType);
                Metadata.InvokeConfigure(module, configuredType, dependency);
            }

            _byType[moduleType] = module;
            _sorted.Add(module);
        }
        finally
        {
            _resolving.Remove(moduleType);
        }

        return module;
    }

    public Modules Configure<TComponent>(TComponent component) where TComponent : notnull
    {
        if (component is Module)
        {
            throw new HexedException.InvalidConfiguration(
                $"Cannot configure module {typeof(TComponent).TypeName()}, use Load() instead");
        }

        foreach (var target in _sorted.OfType<Configure<TComponent>>())
        {
            Metadata.InvokeConfigure(target, typeof(TComponent), component);
        }

        return this;
    }

    public IEnumerator<Module> GetEnumerator() => _sorted.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_sorted).GetEnumerator();

    public int Count => _sorted.Count;
}