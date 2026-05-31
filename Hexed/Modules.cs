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
        set => field = value;
        get => field ??= new Metadata.Reflection();
    }
    
    private readonly Glob _glob = new Glob();

    private readonly Dictionary<Type, Module> _byType = new();

    private readonly List<Module> _sorted = new();

    private Module Load(Type moduleType)
    {
        return _byType.TryGetValue(moduleType, out var existing)
            ? existing
            : Load(Metadata.CreateModule(moduleType));
    }

    public TModule Load<TModule>() where TModule : Module, new() =>
        (TModule)Load(typeof(TModule));

    public TModule Load<TModule>(TModule module) where TModule : Module
    {
        var moduleType = module.GetType();

        if (_byType.TryGetValue(moduleType, out var existing))
            throw new Exception(
                $"Attempted to register {moduleType.TypeName()} via Load(instance) after it was already loaded. Register the instance higher up in the dependency tree, before modules that depend on it are loaded.");
        
        var globbedModules = Metadata.GlobbedModules(moduleType).ToArray();

        foreach (var usedType in Metadata.UsedModules(moduleType))
        {
            if (Metadata.UsedModules(usedType).Contains(moduleType))
            {
                throw new Exception(
                    $"Circular dependency between {moduleType.TypeName()} and {usedType.TypeName()}");
            }

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

        return module;
    }

    public Modules Configure<TComponent>(TComponent component) where TComponent : notnull
    {
        if (component is Module)
        {
            throw new InvalidOperationException(
                $"Cannot configure module {typeof(TComponent).TypeName()}, use Load() instead");
        }

        foreach (var target in _sorted.OfType<Configure<TComponent>>())
        {
            Metadata.InvokeConfigure(target, component.GetType(), component);
        }

        return this;
    }

    public IEnumerator<Module> GetEnumerator() => _sorted.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_sorted).GetEnumerator();

    public int Count => _sorted.Count;
}