using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Hexed.AspNetCore.Test;

public sealed class ExtensionsTest
{
    [Fact]
    public void Build_ReturnsWebApplication()
    {
        var modules = new Modules();
        modules.Load<EmptyModule>();

        using var app = modules.Build();

        app.Should().NotBeNull();
    }

    [Fact]
    public void Build_ModuleExtension_ReturnsWebApplication()
    {
        using var app = new EmptyModule().Build();

        app.Should().NotBeNull();
    }

    [Fact]
    public void Build_UsesModuleConfiguration()
    {
        var modules = new Modules();
        modules.Load<GreetingModule>();

        using var app = modules.Build();

        var greeting = app.Services.GetService<GreetingService>();

        greeting.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_WithCustomRunner_ReturnsResult()
    {
        var modules = new Modules();
        modules.Load(new RunnerModule((_, _) => Task.FromResult(42)));

        var result = await modules.RunAsync();

        result.Should().Be(42);
    }

    [Fact]
    public async Task RunAsync_WithCustomRunner_ReceivesWebApplication()
    {
        WebApplication? receivedApp = null;
        var modules = new Modules();
        modules.Load(new RunnerModule((app, _) =>
        {
            receivedApp = app;
            return Task.FromResult(0);
        }));

        await modules.RunAsync();

        receivedApp.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_CustomRunner_ReceivesArgs()
    {
        var modules = new Modules();
        modules.Load(new RunnerModule((_, args) =>
        {
            args.Should().Contain("--test");
            return Task.FromResult(0);
        }));

        await modules.RunAsync(new[] { "--test" });
    }

    public sealed class EmptyModule : Module;

    public sealed class GreetingService;

    public sealed class GreetingModule : Configure<IServiceCollection>
    {
        public void Configure(IServiceCollection services)
        {
            services.AddSingleton(new GreetingService());
        }
    }

    public sealed class RunnerModule : Configure<IServiceCollection>
    {
        private readonly Func<WebApplication, string[], Task<int>> _runner;

        public RunnerModule(Func<WebApplication, string[], Task<int>> runner)
        {
            _runner = runner;
        }

        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<RunWebApplication>(_ => (app, args) => _runner(app, args));
        }
    }
}
