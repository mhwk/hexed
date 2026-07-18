using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Hexed.Analyzer.Test;

public static class GeneratorTestHelper
{
    public static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, string? GeneratedSource) Run(string source)
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
}