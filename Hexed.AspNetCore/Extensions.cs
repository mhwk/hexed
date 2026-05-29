using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hexed.AspNetCore;

public static class Extensions
{
    public static WebApplication Build(this Modules modules, params string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        modules.Configure(builder);
        modules.Configure(builder.Configuration);
        modules.Configure(builder.Services);

        var app = builder.Build();

        modules.Configure(app);
        modules.Configure(app.Services);

        return app;
    }

    public static WebApplication Build(this Module module, params string[] args)
    {
        var modules = new Modules();

        modules.Load(module);

        return modules.Build(args);
    }

    public static async Task<int> RunAsync(this Modules modules, params string[] args)
    {
        await using var app = modules.Build(args);

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        try
        {
            await app.RunAsync();
            return 0;
        }
        catch (OperationCanceledException) when (lifetime.ApplicationStopping.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception error)
        {
            app.Logger.LogError(error, "Application crash");
            return -1;
        }
    }

    public static async Task<int> RunAsync(this Module module, params string[] args)
    {
        var modules = new Modules();

        modules.Load(module);

        return await modules.RunAsync(args);
    }
}