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

        a.Should().Throw<Exception.CircularDependency>()
            .WithMessage($"Circular dependency between {typeof(CircularUseA).TypeName()} and {typeof(CircularUseB).TypeName()}");
        b.Should().Throw<Exception.CircularDependency>()
            .WithMessage($"Circular dependency between {typeof(CircularUseB).TypeName()} and {typeof(CircularUseA).TypeName()}");
    }

    [Fact]
    public void CircularConfigureThrowsException()
    {
        var modules = new Modules();

        var a = () => modules.Load<CircularConfigureA>();
        var b = () => modules.Load<CircularConfigureB>();

        a.Should().Throw<Exception.CircularDependency>()
            .WithMessage(
                $"Circular dependency between {typeof(CircularConfigureA).TypeName()} and {typeof(CircularConfigureB).TypeName()}");
        b.Should().Throw<Exception.CircularDependency>()
            .WithMessage(
                $"Circular dependency between {typeof(CircularConfigureB).TypeName()} and {typeof(CircularConfigureA).TypeName()}");
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
    public void ConfigureThrowsForModuleType()
    {
        var modules = new Modules();

        var act = () => modules.Configure(new ModuleA());

        act.Should().Throw<Exception.InvalidConfiguration>()
            .WithMessage($"Cannot configure module {typeof(ModuleA).TypeName()}, use Load() instead");
    }

    [Fact]
    public void LoadInstanceAfterTypeLoadThrows()
    {
        var modules = new Modules();
        modules.Load<ModuleA>();

        var act = () => modules.Load(new ModuleA());

        act.Should().Throw<Exception.ModuleAlreadyRegistered>()
            .WithMessage(
                $"Attempted to register {typeof(ModuleA).TypeName()} via Load(instance) after it was already loaded. Register the instance higher up in the dependency tree, before modules that depend on it are loaded.");
    }

    [Fact]
    public void LoadInstanceAfterInstanceLoadThrows()
    {
        var modules = new Modules();
        modules.Load(new ModuleA());

        var act = () => modules.Load(new ModuleA());

        act.Should().Throw<Exception.ModuleAlreadyRegistered>()
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

        load.Should().Throw<Exception.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(DeepCircularA).TypeName()}");
    }

    [Fact]
    public void SelfConfigureThrowsOnCircularDependency()
    {
        var modules = new Modules();

        var load = () => modules.Load<SelfConfiguring>();

        load.Should().Throw<Exception.CircularDependency>()
            .WithMessage($"Circular dependency detected involving {typeof(SelfConfiguring).TypeName()}");
    }

    [Fact]
    public void ConfiguresViaInterface()
    {
        var modules = new Modules();
        modules.Load<ModuleWithConcrete>();

        var configurator = new ConcreteConfigurable();
        modules.Configure<IConfigurable>(configurator);

        configurator.Configured.Should().BeTrue();
    }

    private sealed class ModuleA : Module;

    private sealed class ModuleB : Use<ModuleA>
    {
        public bool Something { get; private set; }

        public ModuleB WithSomething()
        {
            Something = true;

            return this;
        }
    }

    private sealed class ModuleC : Configure<ModuleB>
    {
        public void Configure(ModuleB component)
        {
            component.WithSomething();
        }
    }

    private sealed class ModuleD : Use<ModuleC>, Configure<Component>
    {
        public void Configure(Component component)
        {
            component.WithSomething();
        }
    }

    private sealed class Component
    {
        public bool Something { get; private set; }

        public Component WithSomething()
        {
            Something = true;

            return this;
        }
    }

    private sealed class CircularUseA : Use<CircularUseB>;

    private sealed class CircularUseB : Use<CircularUseA>;

    private sealed class CircularConfigureA : Use<CircularConfigureB>;

    private sealed class CircularConfigureB : Use<CircularConfigureA>;

    private sealed class Globbing : Glob<Globbed>;

    private sealed class Globbed : Module;

    private sealed class DeepCircularA : Use<DeepCircularB>;

    private sealed class DeepCircularB : Use<DeepCircularC>;

    private sealed class DeepCircularC : Use<DeepCircularA>;

    private sealed class SelfConfiguring : Configure<SelfConfiguring>
    {
        public void Configure(SelfConfiguring component)
        {
        }
    }

    private interface IConfigurable;

    private sealed class ConcreteConfigurable : IConfigurable
    {
        public bool Configured { get; set; }
    }

    private sealed class ModuleWithConcrete : Configure<IConfigurable>
    {
        public void Configure(IConfigurable component)
        {
            ((ConcreteConfigurable)component).Configured = true;
        }
    }
}