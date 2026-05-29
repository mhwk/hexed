# Hexed

> Become a victim of your insanity.

Hexed is a lightweight module loader for .NET. Define your application as a collection of modules, declare what they depend on, and let Hexed wire them together in the right order.

## Getting started

Install the package:

```sh
dotnet add package Hexed
```

Define a module:

```csharp
public sealed class MyModule : Configure<IServiceCollection>
{
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<MyService>();
    }
}
```

Run your application:

```csharp
return await new MyModule().RunAsync(args);
```

When you call `RunAsync`, Hexed resolves the full dependency graph of your module — loading dependencies first, in order, before your module runs. By the time `Configure` is called on your module, everything it depends on is already in place.

## Module dependencies

Each module defines its own dependencies. There are three ways of doing this.

### `Use<TModule>`

Implement `Use<TModule>` if your module depends on the components loaded by `TModule`, and needs no further configuration.

```csharp
public sealed class SomeModule : Use<SomeOtherModule>;
```

### `Glob<TModule>`

Implement `Glob<TModule>` if your module optionally depends on `TModule`, and you want to control whether it is loaded at runtime using the `HEXED` environment variable.

```csharp
public sealed class SomeModule : Glob<SomeOtherModule>;
```

The `HEXED` environment variable accepts a semicolon-separated list of glob patterns matched against the full type name of each `Glob<TModule>` dependency:

```sh
export HEXED=Example.*;AnotherExample.*
```

If `HEXED` is not set, all `Glob<T>` dependencies are loaded as normal.

### `Configure<TComponent>`

Implement `Configure<TComponent>` if your module needs to configure `TComponent` directly.

If `TComponent` is a module, it is loaded first — similar to `Use<TModule>` — and then `Configure` is called with it passed in.

If `TComponent` is not a module, it is provided by the context you run your application in. For example, the `Hexed.AspNetCore` context provides `IServiceCollection` and `WebApplicationBuilder`. If no context provides the component, the `Configure` method is simply not called — which allows a single module to support multiple contexts.

```csharp
public sealed class SomeModule :
    Configure<SomeOtherModule>,
    Configure<IServiceCollection>
{
    public void Configure(SomeOtherModule module)
    {
        module.WithSomething();
    }

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<MyService>();
    }
}
```

## Native AOT

By default, Hexed uses a reflection-based descriptor to resolve module dependencies. To support native AOT, install `Hexed.Analyzer` alongside `Hexed`:

```sh
dotnet add package Hexed.Analyzer
```

`Hexed.Analyzer` generates a reflection-free descriptor at compile time and registers it automatically. Your entry point stays unchanged.

### Compatibility

| Package                    | Native AOT |
|----------------------------|------------|
| `Hexed`                    | ✓¹         |
| `Hexed.Analyzer`           | ✓          |
| `Hexed.AspNetCore`         | ✓          |
| `Hexed.AspNetCore.OpenApi` | ✓          |
| `Hexed.Critter`            | ✓          |

¹ Requires `Hexed.Analyzer` for native AOT support.

## Contexts

Depending on the framework you're running in, different components are available to `Configure<TComponent>`.

### AspNetCore

Install the `Hexed.AspNetCore` package to use the ASP.NET Core context:

```sh
dotnet add package Hexed.AspNetCore
```

Use the `Build()` or `RunAsync()` method from this namespace on your module:

```csharp
using Hexed.AspNetCore;

return await new MyModule().RunAsync();
```

#### Configurable components

The following components are supported (including their subclasses or interfaces):

- `Microsoft.AspNetCore.Builder.WebApplicationBuilder`
- `Microsoft.AspNetCore.Builder.WebApplication`
- `Microsoft.Extensions.Configuration.IConfiguration`
- `Microsoft.Extensions.DependencyInjection.IServiceCollection`
- `System.IServiceProvider`