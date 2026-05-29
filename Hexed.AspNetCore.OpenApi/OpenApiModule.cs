using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Hexed.AspNetCore.OpenApi;

public sealed class OpenApiModule : Configure<WebApplicationBuilder>, Configure<WebApplication>
{
    private Func<IHostEnvironment, bool> _enabledEnvironment = env => env.IsDevelopment();

    public OpenApiModule WithEnabledEnvironment(Func<IHostEnvironment, bool> enabledEnvironment)
    {
        _enabledEnvironment = enabledEnvironment;

        return this;
    }
    
    public void Configure(WebApplicationBuilder component)
    {
        if (!_enabledEnvironment.Invoke(component.Environment))
        {
            return;
        }
        
        component.Services.AddOpenApi();
    }

    public void Configure(WebApplication component)
    {
        if (!_enabledEnvironment.Invoke(component.Environment))
        {
            return;
        }
        
        component.MapOpenApi();
        component.MapScalarApiReference();
    }
}