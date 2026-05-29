using Hexed.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hexed.AspNetCore;

public static class Extensions
{
    extension(Module module)
    {
        public WebApplication Build(params string[] args)
        {
            var modules = new Modules();
            
            modules.Load(module);
            
            return modules.Build(args);
        }

        public Task<int> RunAsync(params string[] args)
        {
            var modules = new Modules();
            
            modules.Load(module);
            
            return modules.RunAsync(args);
        }
    }
    
    extension(Modules modules)
    {
        public WebApplication Build(params string[] args)
        {
            Console.WriteLine(modules.ToMermaid());
            
            var builder = WebApplication.CreateBuilder(args);
            
            modules.Configure(builder);
            modules.Configure(builder.Configuration);
            modules.Configure(builder.Services);
            
            var application = builder.Build();
            
            modules.Configure(application);
            modules.Configure(application.Services);

            return application;
        }

        public async Task<int> RunAsync(params string[] args)
        {
            await using var application = modules.Build(args);
            
            var runner = application.Services.GetService<RunWebApplication>() ?? RunDefaultWebApplication;
            
            return await runner.Invoke(application, args);
        }
    }

    private static async Task<int> RunDefaultWebApplication(WebApplication app, string[] args)
    {
        try
        {
            await app.RunAsync();
            return 0;
        }
        catch (Exception error)
        {
            app.Logger.LogCritical(error, "Uncaught exception");
            return -1;
        }
    }
}