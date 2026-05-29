# Hexed — Agent Guide

## Projects

| Project | Target | Purpose |
|---------|--------|---------|
| `Hexed/` | `net10.0` | Core library — `Module`, `Use<T>`, `Glob<T>`, `Configure<T>`, `Modules`, `ModuleDescriptor` |
| `Hexed.AspNetCore/` | `net10.0` | ASP.NET Core integration — `Build()`, `RunAsync()` extensions |
| `Hexed.AspNetCore.OpenApi/` | `net10.0` | Optional OpenAPI/Scalar module |
| `Hexed.Analyzer/` | `netstandard2.0` | Roslyn source generator (`IIncrementalGenerator`) for AOT-safe descriptor |
| `Hexed.Test/` | `net10.0` | xUnit tests for core |
| `Hexed.Analyzer.Test/` | `net10.0` | xUnit tests for source generator |
| `Example/` | `net10.0` | Sample app — references `Hexed.Analyzer` as analyzer (not assembly ref) |

## Build & Test

```powershell
dotnet build                           # builds all projects
dotnet test                            # runs all tests
dotnet test Hexed.Test                 # focused run
dotnet test Hexed.Analyzer.Test        # generator tests only
dotnet pack                            # produces NuGet packages (.nupkg)
dotnet restore                         # restore packages
```

No special order required (no codegen step before build). The source generator runs as part of normal compilation.

## Architecture

- **Marker-interface DI**: Dependencies declared via interface inheritance (`Use<T>`, `Glob<T>`, `Configure<T>`), not constructor injection or attributes.
- **Two-phase resolution**: (1) Load modules & resolve `Use<T>`/`Glob<T>`/`Configure<TModule>` dependencies. (2) Push non-module components (e.g., `WebApplicationBuilder`) via `Modules.Configure(TComponent)`.
- **Glob-based conditional loading**: `Glob<T>` loads only if type name matches `HEXED` env var (semicolon-separated glob patterns). Unset `HEXED` → all globs loaded.
- **AOT safety**: `Hexed.Analyzer` generates `GeneratedModuleDescriptor` + `[ModuleInitializer]` at compile time so runtime reflection is unnecessary. Both `Hexed` and `Hexed.AspNetCore` are `IsAotCompatible=true`.
- **Entrypoints**: `new MyModule().RunAsync(args)` (AspNetCore), or manually `new Modules().Load(module).Configure(component)` for custom hosts.

## Code conventions (from .editorconfig)

- No implicit usings in library projects (disabled in `common.props`); enabled only in `Hexed.AspNetCore.OpenApi`
- File-scoped namespaces, primary constructors preferred
- Explicit types preferred (`var` disabled for built-in, apparent, and elsewhere)
- Expression-bodied: accessors/indexers/properties yes; methods/constructors/local-functions no
- Private fields: `_camelCase`; private static fields: `s_camelCase`
- Public/constant/static-readonly fields: `PascalCase`
- `readonly` fields enforced as warnings
- Braces always required (`csharp_prefer_braces = true`)
- Line length 128, indent 4 spaces (CS), 2 spaces (XML/JSON)
- `dotnet_sort_system_directives_first = false` — System directives not sorted first

## Shared build properties (`.props/`)

- `common.props`: `net10.0`, C# 12, nullable enabled, implicit usings disabled.
- `test.props`: imports `common.props` + xUnit, AwesomeAssertions (not FluentAssertions), coverlet, Microsoft.NET.Test.Sdk.
- `library.props`: imports `common.props` (base for libraries).
- `analyzer.props`: imports `library.props` + `Microsoft.CodeAnalysis.CSharp 4.*`, `IsRoslynComponent=true`.

## Testing quirks

- **xUnit** + **AwesomeAssertions** (not FluentAssertions, not Shouldly).
- `Hexed.Test` uses `InternalsVisibleTo` (from `Hexed.csproj`) to test internal types.
- `Hexed.Analyzer.Test` uses `GeneratorTestHelper.Run(source)` — parses C# source, runs the generator via `CSharpGeneratorDriver`, returns generated code and diagnostics.
- `Glob` tests rely on setting the `HEXED` environment variable.

## Key `Modules` API

```csharp
modules.Load<T>()                      // resolve & load module + transitive deps
modules.Load<T>(T instance)            // register pre-created instance
modules.Configure<T>(T component)      // push component through Configure<T> implementations
```

## Mermaid visualization

`modules.ToMermaid()` outputs a `graph TD` Mermaid flowchart of the dependency graph.

## Generated code

- Output: `GeneratedModuleDescriptor` (hardcoded switch-case for all descriptor methods) + `HexedInitializer` (sets `Modules.Descriptor` via `[ModuleInitializer]`).
- Generated code is not committed; it is produced at build time by `Hexed.Analyzer`.
