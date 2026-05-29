using Hexed.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hexed;

public sealed class Modules : IReadOnlyCollection<Module>
{
    public static Descriptor Descriptor
    {
        set => field = value;
        get => field ??= new Descriptor.Reflection();
    }

    private readonly Dictionary<Type, Module> _byType = new();

    private readonly List<Module> _sorted = new();

    private Module Load(Type moduleType)
    {
        return _byType.TryGetValue(moduleType, out var existing)
            ? existing
            : Load(Descriptor.CreateModule(moduleType));
    }

    public TModule Load<TModule>() where TModule : Module, new() =>
        (TModule)Load(typeof(TModule));

    public TModule Load<TModule>(TModule module) where TModule : Module
    {
        var moduleType = module.GetType();

        if (_byType.TryGetValue(moduleType, out var existing))
            return (TModule)existing;
        
        var globbedModules = Descriptor.GlobbedModules(moduleType).ToArray();

        foreach (var usedType in Descriptor.UsedModules(moduleType))
        {
            if (Descriptor.UsedModules(usedType).Contains(moduleType))
            {
                throw new Exception(
                    $"Circular dependency between {moduleType.TypeName()} and {usedType.TypeName()}");
            }

            if (globbedModules.Contains(usedType) && !Glob.IsMatch(usedType))
            {
                continue;
            }

            Load(usedType);
        }

        foreach (var configuredType in Descriptor.ConfiguredModules(moduleType))
        {
            var dependency = Load(configuredType);
            Descriptor.InvokeConfigure(module, configuredType, dependency);
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
            Descriptor.InvokeConfigure(target, component.GetType(), component);
        }

        return this;
    }

    public IEnumerator<Module> GetEnumerator() => _sorted.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_sorted).GetEnumerator();

    public int Count => _sorted.Count;
}