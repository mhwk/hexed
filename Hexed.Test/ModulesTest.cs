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
        Glob.ResetHexed();
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

        a.Should().Throw<Exception>()
            .WithMessage($"Circular dependency between {typeof(CircularUseA).TypeName()} and {typeof(CircularUseB).TypeName()}");
        b.Should().Throw<Exception>()
            .WithMessage($"Circular dependency between {typeof(CircularUseB).TypeName()} and {typeof(CircularUseA).TypeName()}");
    }

    [Fact]
    public void CircularConfigureThrowsException()
    {
        var modules = new Modules();

        var a = () => modules.Load<CircularConfigureA>();
        var b = () => modules.Load<CircularConfigureB>();

        a.Should().Throw<Exception>()
            .WithMessage(
                $"Circular dependency between {typeof(CircularConfigureA).TypeName()} and {typeof(CircularConfigureB).TypeName()}");
        b.Should().Throw<Exception>()
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
}