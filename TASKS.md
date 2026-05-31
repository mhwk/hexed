# Hexed — Tasks

Check off items as they are completed.

---

## Bugs

- [x] **Source generator emits invalid C# for open generic module definitions** (`Hexed.Analyzer/MetadataGenerator.cs:35-36`) — Open generic definitions (`SomeGeneric<T>`) pass `GetModuleType` and get emitted as invalid `typeof(SomeGeneric<T>)`. Fixed by filtering out `IsGenericType && IsDefinition` in `GetModuleType`.
- [x] **`InvokeConfigure` NRE on missing public method, and wrapped exceptions** (`Hexed/Metadata.cs:66-79`) — Replaced `!` null-forgiving with proper null check and `HexedException.UnknownConfigureInvocation`. Added try/catch to unwrap `TargetInvocationException` via `ExceptionDispatchInfo` preserving original stack trace.
- [x] **`InvalidCastException` in test helper** — Renamed `ModuleWithConcrete` → `ModuleWithInterface`. Added `ModuleWithConcreteDirect` + `ConfiguresViaConcreteType` test for cast-free concrete type configuration.
- [x] **`HexedException` inner exceptions lost** (`Hexed/Exception.cs:13-25`) — None of the nested exception types expose the `(string?, Exception?)` constructor from the base class.
- [x] **Unused exception types** (`Hexed/Exception.cs:21,23`) — Not a bug. Both types are thrown: `UnknownModule` in generated `CreateModule`, `UnknownConfigureInvocation` in generated `InvokeConfigure` and `Metadata.Reflection.InvokeConfigure`.

## Design Issues

- [x] **`Console.WriteLine(ToMermaid())` in production code** (`Hexed.AspNetCore/Extensions.cs:37`) — Intentional, kept as-is.
- [ ] **No thread safety on `Modules`** (`Hexed/Modules.cs:22-28`) — `_byType`, `_sorted`, `_resolving` are unprotected. Document as not thread-safe (or add concurrency protection).
- [ ] **Static `Modules.Metadata` and `Glob` lazy init race** (`Hexed/Modules.cs:17`, `Hexed/Glob.cs:12-22`) — `field ??= expr` without synchronization can create multiple instances.
- [ ] **`Configure<TComponent>` linear scan** (`Hexed/Modules.cs:121`) — `_sorted.OfType<Configure<TComponent>>()` enumerates all loaded modules each time. Consider a `Dictionary<Type, List<Module>>` lookup.
- [x] **Circular dependency check triggers O(N²) reflection** (`Hexed/Modules.cs:82-86`) — Removed the redundant reflection check. Cycle detection relies solely on the `_resolving` guard, which catches all cycles with no reflection overhead.
- [ ] **Redundant `class` constraint** (`Hexed/Use.cs:1`, `Hexed/Glob.cs:7`) — `where TModule : class, Module` — `class` is implied by `Module` being an interface.

## Performance

- [ ] **Regex allocated per `MatchesGlob` call** (`Hexed/Glob.cs:24-30`) — Cache compiled `Regex` objects instead of creating new ones on every pattern match.
- [ ] **`GetInterfaces()` / `GetGenericArguments()` uncached** (`Hexed/Metadata.cs:75-77`) — Every metadata query re-scans interfaces via reflection.
- [ ] **`Intersect().ToArray()` allocates even for conflict-free modules** (`Hexed/Modules.cs:64,72`) — The common case allocates arrays that are immediately discarded.
- [ ] **Missing C# primitive keyword aliases in `TypeName()`** (`Hexed/Text/TypeExtensions.cs:12-19`) — `byte`, `sbyte`, `ushort`, `uint`, `ulong`, `char`, `decimal`, `nint`, `nuint`, `object` produce `System.X` instead of the keyword.

## Missing XML Documentation

- [ ] **`Module.cs`** — root marker interface
- [ ] **`Use.cs`** — dependency declaration
- [ ] **`Glob.cs`** — conditional dependency interface
- [ ] **`Configure.cs`** — configuration contract (document dual role)
- [ ] **`Modules.cs`** — central orchestrator class
- [ ] **`Modules.cs:11`** — `Metadata` static property
- [ ] **`Modules.cs:46`** — `Load<TModule>(TModule)` instance overload
- [ ] **`Modules.cs:113`** — `Configure<TComponent>` method
- [ ] **`Exception.cs`** — all public exception types

## Testing Gaps

- [ ] **No ASP.NET Core integration tests** — `Build()` / `RunAsync()` are untested beyond basic happy paths.
- [ ] **No `ToMermaid()` tests** — zero coverage for the visualization output.
- [ ] **No `Modules.Metadata` static property tests** — setter, getter, and fallback to `Reflection` are uncovered.
- [ ] **Source generator has only 1 test** — missing: empty source, generic modules, abstract modules, no dependencies, many dependencies.
- [ ] **Glob `?` wildcard never tested** — only `*` patterns are used in test cases.
- [ ] **`HEXED` env var is process-global, not isolated** — constructor sets it to `null` but parallel execution would cause flakiness.
- [ ] **Topological order test uses hardcoded indices** — should verify partial order instead of exact positioning.

## Housekeeping

- [x] **Remove unused `Microsoft.Extensions.Configuration` and `Microsoft.Extensions.DependencyInjection` package references** from `Hexed/Hexed.csproj:9-10` (they are never used in source code).
- [ ] **Fix inconsistent `using` style** in `Hexed/Exception.cs:3` — uses `System.Exception` instead of `using System;`.
- [ ] **Add `CancellationToken` checks** in `Hexed.Analyzer/MetadataGenerator.cs` — `Execute()` never checks `ctx.CancellationToken`.
