using Hexed;
using Microsoft.AspNetCore.Builder;

namespace Example;

public sealed class HelloWorld : Configure<WebApplication>
{
    public void Configure(WebApplication component)
    {
        component.MapGet("/", () => "Hello World!");
    }
}