# Hexed

> Become victim of your own insanity.

Hexed simply wires your dependencies together. Ward off the chaos that must come.

## Getting started

Install the package:

```sh
dotnet add package Hexed
```

Define modules and their dependencies:

```csharp
public sealed class MyModule : Use<MyDependency>;
public sealed class MyDependency : Module;
```

Load your module — dependencies are resolved and loaded automatically:

```csharp
var modules = new Modules();
modules.Load<MyModule>();
```

## Module dependencies

Each module defines its own dependencies. There are three ways of doing this.

### `Use<TModule>`

Implement `Use<TModule>` if your module depends on the components configured by `TModule`, and needs no further configuration.

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

Patterns prepended with `!` act as exclusion rules — if any exclusion pattern matches, the module is not loaded regardless of inclusion matches.

```sh
export HEXED=Example.*;!Example.SomeModule
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

Hexed is native aot compatible, unless the integrated library is not.

### Compatibility

| Package                    | Native AOT |
|----------------------------|------------|
| `Hexed`                    | ✓          |
| `Hexed.AspNetCore`         | ✓          |
| `Hexed.AspNetCore.OpenApi` | ✓          |
| `Hexed.Critter`            | ✓          |

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