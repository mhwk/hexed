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
                     public sealed class GlobbingModule : Glob<GlobbedModule> { }
                     public sealed class GlobbedModule : Module { }
                     public sealed class ConfiguringModule : Use<ModuleViaConfigure>, Configure<ModuleViaConfigure>, Configure<SomeComponent>
                     {
                         public void Configure(ModuleViaConfigure component) { }
                         public void Configure(SomeComponent component) { }
                     }
                     public sealed class ModuleViaConfigure : Module { }
                     public sealed class SomeComponent { }
                     """;

        var (_, diagnostics, generatedSource) = GeneratorTestHelper.Run(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Snapshot.AssertGeneratedSourceMatches(generatedSource!);
    }
}