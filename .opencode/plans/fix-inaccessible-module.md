# Fix: Source generator emits `typeof()` for inaccessible types

## Problem

The source generator in `Hexed.Analyzer/MetadataGenerator.cs` emits `typeof(T)` in `UsedModules`, `GlobbedModules`, `ConfiguredModules`, `ConfiguredComponents` arrays and `case` patterns in the `Configure` delegate for ALL discovered dependency types, without checking whether those types are **accessible** from the current compilation. When a type is `internal` to a different assembly, the generated code fails with CS0122.

## Changes

### 1. `Hexed.Analyzer/MetadataGenerator.cs`

**Add helper method** after `IsModule` (after line 185):

```csharp
private static bool IsAccessible(INamedTypeSymbol type, IAssemblySymbol assembly)
{
    for (var current = type; current is not null; current = current.ContainingType)
    {
        var accessibility = current.DeclaredAccessibility;
        if (accessibility != Accessibility.Public && accessibility != Accessibility.Internal)
            return false;
        if (accessibility == Accessibility.Internal &&
            !SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, assembly))
            return false;
    }
    return true;
}
```

**Modify `Execute` to filter lists by accessibility** (lines 128-136 and 155-163):

Replace lines 128-136:
```csharp
var used = GetInterfaceArguments(module, useType)
    .Where(t => IsModule(t, moduleType))
    .ToList();
var globbed = GetInterfaceArguments(module, globType)
    .Where(t => IsModule(t, moduleType))
    .ToList();
var allConfigured = GetInterfaceArguments(module, configureType).ToList();
var configuredModules = allConfigured.Where(t => IsModule(t, moduleType)).ToList();
var components = allConfigured.Where(t => !IsModule(t, moduleType)).ToList();
```

With:
```csharp
var used = GetInterfaceArguments(module, useType)
    .Where(t => IsModule(t, moduleType))
    .Where(t => IsAccessible(t, compilation.Assembly))
    .ToList();
var globbed = GetInterfaceArguments(module, globType)
    .Where(t => IsModule(t, moduleType))
    .Where(t => IsAccessible(t, compilation.Assembly))
    .ToList();
var allConfigured = GetInterfaceArguments(module, configureType).ToList();
var accessibleConfigured = allConfigured
    .Where(t => IsAccessible(t, compilation.Assembly))
    .ToList();
var configuredModules = accessibleConfigured.Where(t => IsModule(t, moduleType)).ToList();
var components = accessibleConfigured.Where(t => !IsModule(t, moduleType)).ToList();
```

Also on line 160, change `allConfigured` → `accessibleConfigured`:
```csharp
if (accessibleConfigured.Count > 0)
```
and
```csharp
foreach (var configured in accessibleConfigured)
```

### 2. `Hexed.Analyzer.Test/GeneratorTestHelper.cs`

Add new methods:

```csharp
public static MetadataReference CompileAssembly(string source, string assemblyName = "LibraryAssembly")
{
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        .Select(a => MetadataReference.CreateFromFile(a.Location))
        .Cast<MetadataReference>()
        .ToList();
    references.Add(MetadataReference.CreateFromFile(typeof(Hexed.Module).Assembly.Location));

    var compilation = CSharpCompilation.Create(
        assemblyName: assemblyName,
        syntaxTrees: [syntaxTree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    using var ms = new MemoryStream();
    var result = compilation.Emit(ms);
    if (!result.Success)
    {
        var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        throw new InvalidOperationException($"Library compilation failed:\n{errors}");
    }
    ms.Position = 0;
    return MetadataReference.CreateFromStream(ms);
}

public static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, string? GeneratedSource) Run(
    string source,
    params MetadataReference[] additionalReferences)
{
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        .Select(a => MetadataReference.CreateFromFile(a.Location))
        .Cast<MetadataReference>()
        .ToList();
    references.Add(MetadataReference.CreateFromFile(typeof(Hexed.Module).Assembly.Location));
    references.AddRange(additionalReferences);

    var compilation = CSharpCompilation.Create(
        assemblyName: "TestAssembly",
        syntaxTrees: [syntaxTree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new MetadataGenerator();

    var driver = CSharpGeneratorDriver
        .Create(generator)
        .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

    var generatedSource = driver
        .GetRunResult()
        .GeneratedTrees
        .FirstOrDefault(t => t.FilePath.EndsWith("GeneratedMetadata.g.cs"))
        ?.GetText()
        .ToString();

    return (outputCompilation, diagnostics, generatedSource);
}
```

Also add `using System.IO;` and `using System;` to the imports if not present.

### 3. `Hexed.Analyzer.Test/MetadataGeneratorTest.cs`

Add three new tests:

```csharp
[Fact]
public void ExcludesInaccessibleModuleFromUsedModules()
{
    var libSource = """
        using Hexed;

        namespace Lib;

        internal sealed class InternalModule : Module { }

        public sealed class GenericModule<TModule> : Use<InternalModule> { }
        """;

    var libReference = GeneratorTestHelper.CompileAssembly(libSource);
    var consumerSource = """
        using Hexed;

        public sealed class MyModule : Use<Lib.GenericModule<MyModule>> { }
        """;

    var (_, diagnostics, generatedSource) = GeneratorTestHelper.Run(consumerSource, libReference);

    Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    Assert.Contains("typeof(global::Lib.GenericModule<global::MyModule>)", generatedSource);
    Assert.DoesNotContain("typeof(global::Lib.InternalModule)", generatedSource);
}

[Fact]
public void IncludesPublicModuleFromOtherAssemblyInUsedModules()
{
    var libSource = """
        using Hexed;

        namespace Lib;

        public sealed class PublicModule : Module { }
        """;

    var libReference = GeneratorTestHelper.CompileAssembly(libSource);
    var consumerSource = """
        using Hexed;

        public sealed class MyModule : Use<Lib.PublicModule> { }
        """;

    var (_, diagnostics, generatedSource) = GeneratorTestHelper.Run(consumerSource, libReference);

    Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    Assert.Contains("typeof(global::Lib.PublicModule)", generatedSource);
}

[Fact]
public void IncludesInternalModuleFromSameAssemblyInUsedModules()
{
    var source = """
        using Hexed;

        internal sealed class InternalModule : Module { }

        public sealed class MyModule : Use<InternalModule> { }
        """;

    var (_, diagnostics, generatedSource) = GeneratorTestHelper.Run(source);

    Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    Assert.Contains("typeof(global::InternalModule)", generatedSource);
}
```

## Verification

Run `dotnet test Hexed.Analyzer.Test` to confirm:
- New tests pass
- Existing `GeneratesMetadataForSimpleModule` snapshot test still passes (no snapshot changes needed)
