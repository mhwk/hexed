using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
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

        var generator = new DescriptorGenerator();

        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var generatedSource = driver
            .GetRunResult()
            .GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("GeneratedDescriptor.g.cs"))
            ?.GetText()
            .ToString();

        return (outputCompilation, diagnostics, generatedSource);
    }
}