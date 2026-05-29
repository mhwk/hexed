using Microsoft.CodeAnalysis;
using System.Linq;

namespace Hexed.Analyzer.Test;

public class DescriptorGeneratorTest
{
    [Fact]
    public void GeneratesDescriptorForSimpleModule()
    {
        var source = """
                     using Hexed;

                     public sealed class AppModule : Use<OtherModule> { }
                     public sealed class OtherModule : Module { }
                     """;

        var (_, diagnostics, generatedSource) = GeneratorTestHelper.Run(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("typeof(AppModule)", generatedSource);
        Assert.Contains("typeof(OtherModule)", generatedSource);
    }
}