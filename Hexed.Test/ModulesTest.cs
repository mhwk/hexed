using AwesomeAssertions;
using Hexed.Text;
using System;
using System.Linq;

namespace Hexed.Test;

public class ModulesTest
{
    public ModulesTest()
    {
        Environment.SetEnvironmentVariable("HEXED", null);
    }

    [Fact]
    public void EmptyModulesHasZeroCount()
    {
        var modules = new Modules();

        modules.Count.Should().Be(0);
        modules.Should().BeEmpty();
    }

    [Fact]
    public void LoadsDependentModules()
    {
        var modules = new Modules();
        modules.Load<ModuleD>();

        modules.Count.Should().Be(4);
    }

    [Fact]
    public void DependencyModulesLoadedBeforeDependentModule()
    {
        var modules = new Modules();
        modules.Load<ModuleD>();

        var moduleArray = modules.ToArray();
        moduleArray[0].Should().BeOfType<ModuleA>();
        moduleArray[1].Should().BeOfType<ModuleB>();
        moduleArray[2].Should().BeOfType<ModuleC>();
        moduleArray[3].Should().BeOfType<ModuleD>();
    }

    [Fact]
    public void ConfiguresDependentModules()
    {
        var modules = new Modules();
        modules.Load<ModuleD>();

        modules.OfType<ModuleB>().First().Something.Should().BeTrue();
    }

    [Fact]
    public void ConfiguresComponents()
    {
        var modules = new Modules();
        modules.Load<ModuleD>();

        var component = new Component();
        modules.Configure(component);

        component.Something.Should().BeTrue();
    }

    [Fact]
    public void CircularUseThrowsException()
    {
        var modules = new Modules();

        var a = () => modules.Load<CircularUseA>();
        var b = () => modules.Load<CircularUseB>();

        a.Should().Throw<HexedException.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(CircularUseA).TypeName()}");
        b.Should().Throw<HexedException.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(CircularUseB).TypeName()}");
    }

    [Fact]
    public void CircularConfigureThrowsException()
    {
        var modules = new Modules();

        var a = () => modules.Load<CircularConfigureA>();
        var b = () => modules.Load<CircularConfigureB>();

        a.Should().Throw<HexedException.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(CircularConfigureA).TypeName()}");
        b.Should().Throw<HexedException.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(CircularConfigureB).TypeName()}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("*")]
    [InlineData("*.Globbed")]
    [InlineData("Hexed.Test.*")]
    [InlineData("Hexed.Test.*.Globbed")]
    public void GlobbedModuleIsLoaded(string? globPattern)
    {
        Environment.SetEnvironmentVariable("HEXED", globPattern);

        var modules = new Modules();
        modules.Load<Globbing>();
        modules.OfType<Globbed>().Should().HaveCount(1);
    }

    [Theory]
    [InlineData("Foo")]
    [InlineData("Hexed.Test.Foo")]
    public void GlobbedModuleIsNotLoaded(string? globPattern)
    {
        Environment.SetEnvironmentVariable("HEXED", globPattern);

        var modules = new Modules();
        modules.Load<Globbing>();
        modules.OfType<Globbed>().Should().HaveCount(0);
    }

    [Theory]
    [InlineData("!*")]
    [InlineData("!Hexed.Test.ModulesTest.Globbed")]
    [InlineData("!*.Globbed")]
    [InlineData("*;!*.Globbed")]
    [InlineData("!*;*.Globbed")]
    [InlineData("!*.Globbed;*")]
    public void GlobbedModuleIsNotLoadedDueToExclusion(string? globPattern)
    {
        Environment.SetEnvironmentVariable("HEXED", globPattern);

        var modules = new Modules();
        modules.Load<Globbing>();
        modules.OfType<Globbed>().Should().HaveCount(0);
    }

    [Theory]
    [InlineData("!Foo")]
    [InlineData("!Foo.Bar")]
    [InlineData("*;!Foo")]
    public void GlobbedModuleIsLoadedDespiteExclusion(string? globPattern)
    {
        Environment.SetEnvironmentVariable("HEXED", globPattern);

        var modules = new Modules();
        modules.Load<Globbing>();
        modules.OfType<Globbed>().Should().HaveCount(1);
    }

    [Fact]
    public void LoadsPreCreatedModule()
    {
        var modules = new Modules();
        var preCreated = new ModuleB().WithSomething();
        modules.Load(preCreated);

        modules.Count.Should().Be(2);
        modules.OfType<ModuleA>().Should().HaveCount(1);
        modules.OfType<ModuleB>().First().Something.Should().BeTrue();
    }

    [Fact]
    public void LoadSameModuleTwiceReturnsSameInstance()
    {
        var modules = new Modules();
        var first = modules.Load<ModuleD>();
        var second = modules.Load<ModuleD>();

        modules.Count.Should().Be(4);
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void ConfigureExceptionPreservesOriginalType()
    {
        var modules = new Modules();
        modules.Load<ThrowingModule>();

        var act = () => modules.Configure(new Component());

        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void ConfigureThrowsForModuleType()
    {
        var modules = new Modules();

        var act = () => modules.Configure(new ModuleA());

        act.Should().Throw<HexedException.InvalidConfiguration>()
            .WithMessage($"Cannot configure module {typeof(ModuleA).TypeName()}, use Load() instead");
    }

    [Fact]
    public void LoadInstanceAfterTypeLoadThrows()
    {
        var modules = new Modules();
        modules.Load<ModuleA>();

        var act = () => modules.Load(new ModuleA());

        act.Should().Throw<HexedException.ModuleAlreadyRegistered>()
            .WithMessage(
                $"Attempted to register {typeof(ModuleA).TypeName()} via Load(instance) after it was already loaded. Register the instance higher up in the dependency tree, before modules that depend on it are loaded.");
    }

    [Fact]
    public void LoadInstanceAfterInstanceLoadThrows()
    {
        var modules = new Modules();
        modules.Load(new ModuleA());

        var act = () => modules.Load(new ModuleA());

        act.Should().Throw<HexedException.ModuleAlreadyRegistered>()
            .WithMessage(
                $"Attempted to register {typeof(ModuleA).TypeName()} via Load(instance) after it was already loaded. Register the instance higher up in the dependency tree, before modules that depend on it are loaded.");
    }

    [Fact]
    public void ConfigureWithNoMatchingHandlers()
    {
        var modules = new Modules();
        modules.Load<ModuleA>();

        var configure = () => modules.Configure(new object());
        configure.Should().NotThrow();
    }

    [Fact]
    public void DeepCircularUseThrowsException()
    {
        var modules = new Modules();

        var load = () => modules.Load<DeepCircularA>();

        load.Should().Throw<HexedException.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(DeepCircularA).TypeName()}");
    }

    [Fact]
    public void SelfConfigureThrowsOnCircularDependency()
    {
        var modules = new Modules();

        var load = () => modules.Load<SelfConfiguring>();

        load.Should().Throw<HexedException.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(SelfConfiguring).TypeName()}");
    }

    [Fact]
    public void ConfiguresViaInterface()
    {
        var modules = new Modules();
        modules.Load<ModuleWithConfigurableInterface>();

        var configurator = new ConcreteConfigurable();
        modules.Configure<IConfigurable>(configurator);

        configurator.Configured.Should().BeTrue();
    }

    [Fact]
    public void ConfiguresViaConcreteType()
    {
        var modules = new Modules();
        modules.Load<ModuleWithConfigurableConcrete>();

        var configurator = new ConcreteConfigurable();
        modules.Configure(configurator);

        configurator.Configured.Should().BeTrue();
    }

    [Fact]
    public void GlobAndConfigureOnSameTypeThrows()
    {
        var modules = new Modules();

        var load = () => modules.Load<GlobAndConfigureConflict>();

        load.Should().Throw<HexedException.InvalidModuleDeclaration>()
            .WithMessage($"*Glob<{typeof(ModuleForConflict).TypeName()}>*");
    }

    [Fact]
    public void UseAndConfigureOnSameTypeThrows()
    {
        var modules = new Modules();

        var load = () => modules.Load<UseAndConfigureConflict>();

        load.Should().Throw<HexedException.InvalidModuleDeclaration>()
            .WithMessage($"*Use<{typeof(ModuleForConflict).TypeName()}>*");
    }

    [Fact]
    public void ConstructedGenericInheritsArraysFromOpenGeneric()
    {
        Modules.Metadata.Register(typeof(OpenGenericModule<>), new Metadata
        {
            UsedModules = [typeof(DepModule)],
            ConfiguredComponents = [typeof(SomeConfigComponent)],
            Factory = () => throw new InvalidOperationException("Open generic"),
            Configure = (_, _) => { }
        });

        Modules.Metadata.Register(typeof(OpenGenericModule<int>), new Metadata
        {
            Factory = () => new OpenGenericModule<int>(),
            Configure = (_, _) => { }
        });

        var metadata = Modules.Metadata[typeof(OpenGenericModule<int>)];
        metadata.UsedModules.Should().Contain(typeof(DepModule));
        metadata.ConfiguredComponents.Should().Contain(typeof(SomeConfigComponent));
        metadata.Factory.Should().NotBeNull();
    }

    [Fact]
    public void NonGenericDoesNotAttemptOpenGenericLookup()
    {
        Modules.Metadata.Register(typeof(SimpleMetadataModule), new Metadata
        {
            Factory = () => new SimpleMetadataModule(),
            Configure = (_, _) => { }
        });

        var metadata = Modules.Metadata[typeof(SimpleMetadataModule)];
        metadata.Factory.Invoke().Should().BeOfType<SimpleMetadataModule>();
    }

    public sealed class ModuleA : Module;

    public sealed class ModuleB : Use<ModuleA>
    {
        public bool Something { get; private set; }

        public ModuleB WithSomething()
        {
            Something = true;

            return this;
        }
    }

    public sealed class ModuleC : Configure<ModuleB>
    {
        public void Configure(ModuleB component)
        {
            component.WithSomething();
        }
    }

    public sealed class ModuleD : Use<ModuleC>, Configure<Component>
    {
        public void Configure(Component component)
        {
            component.WithSomething();
        }
    }

    public sealed class Component
    {
        public bool Something { get; private set; }

        public Component WithSomething()
        {
            Something = true;

            return this;
        }
    }

    public sealed class CircularUseA : Use<CircularUseB>;

    public sealed class CircularUseB : Use<CircularUseA>;

    public sealed class CircularConfigureA : Use<CircularConfigureB>;

    public sealed class CircularConfigureB : Use<CircularConfigureA>;

    public sealed class Globbing : Glob<Globbed>;

    public sealed class Globbed : Module;

    public sealed class DeepCircularA : Use<DeepCircularB>;

    public sealed class DeepCircularB : Use<DeepCircularC>;

    public sealed class DeepCircularC : Use<DeepCircularA>;

    public sealed class SelfConfiguring : Configure<SelfConfiguring>
    {
        public void Configure(SelfConfiguring component)
        {
        }
    }

    public interface IConfigurable;

    public sealed class ConcreteConfigurable : IConfigurable
    {
        public bool Configured { get; set; }
    }

    public sealed class ModuleWithConfigurableInterface : Configure<IConfigurable>
    {
        public void Configure(IConfigurable component)
        {
            ((ConcreteConfigurable)component).Configured = true;
        }
    }

    public sealed class ModuleWithConfigurableConcrete : Configure<ConcreteConfigurable>
    {
        public void Configure(ConcreteConfigurable component)
        {
            component.Configured = true;
        }
    }

    public sealed class ThrowingModule : Configure<Component>
    {
        public void Configure(Component component)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    public sealed class ModuleForConflict : Module;

    public sealed class GlobAndConfigureConflict : Glob<ModuleForConflict>, Configure<ModuleForConflict>
    {
        public void Configure(ModuleForConflict component)
        {
        }
    }

    public sealed class UseAndConfigureConflict : Use<ModuleForConflict>, Configure<ModuleForConflict>
    {
        public void Configure(ModuleForConflict component)
        {
        }
    }

    public sealed class OpenGenericModule<T> : Module;

    public sealed class DepModule : Module;

    public sealed class SomeConfigComponent;

    public sealed class SimpleMetadataModule : Module;
}