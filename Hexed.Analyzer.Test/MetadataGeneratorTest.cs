using Microsoft.CodeAnalysis;
using System.Linq;

namespace Hexed.Analyzer.Test;

public class MetadataGeneratorTest
{
    [Fact]
    public void GeneratesMetadataForSimpleModule()
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
                     public sealed class Container
                     {
                         public sealed class NestedModule : Module { }
                     }
                     public sealed class ModuleUsingNested : Use<Container.NestedModule> { }
                     """;

        var (_, diagnostics, generatedSource) = GeneratorTestHelper.Run(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Snapshot.AssertGeneratedSourceMatches(generatedSource!);
    }
}