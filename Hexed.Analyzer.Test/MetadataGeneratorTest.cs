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
                      public sealed class SomeGeneric<T> : Module { }
                      public sealed class AnotherModule : Use<SomeGeneric<int>> { }

                      namespace MyApp;
                      public sealed class NamespacedModule : Module { }
                      public sealed class ModuleUsingNamespace : Use<MyApp.NamespacedModule> { }
                     """;

        var (_, diagnostics, generatedSource) = GeneratorTestHelper.Run(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Snapshot.AssertGeneratedSourceMatches(generatedSource!);
    }

    [Fact]
    public void EmitsInternalModuleFromOtherAssemblyInUsedModules()
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
        Assert.Contains("typeof(global::Lib.InternalModule)", generatedSource);
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
}