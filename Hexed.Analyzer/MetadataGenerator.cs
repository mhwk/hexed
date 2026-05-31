using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hexed.Analyzer;

[Generator]
public sealed class MetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var moduleTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetModuleType(ctx, ct))
            .Where(static t => t is not null);

        var compilationAndModules = moduleTypes.Collect()
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(
            compilationAndModules,
            static (ctx, tuple) => Execute(ctx, tuple.Left, tuple.Right));
    }

    private static INamedTypeSymbol? GetModuleType(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var declaration = (ClassDeclarationSyntax)ctx.Node;
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(declaration, ct) as INamedTypeSymbol;

        if (symbol is null || symbol.IsAbstract)
            return null;

        if (symbol.IsGenericType && symbol.IsDefinition)
            return null;

        var moduleType = ctx.SemanticModel.Compilation.GetTypeByMetadataName("Hexed.Module");

        if (moduleType is null)
            return null;

        if (IsModule(symbol, moduleType))
        {
            return symbol;
        }

        return null;
    }

    private static void Execute(SourceProductionContext ctx,
        ImmutableArray<INamedTypeSymbol?> moduleSymbols, Compilation compilation)
    {
        var entryModules = moduleSymbols
            .Where(m => m is not null)
            .Select(m => m!)
            .ToList();

        if (entryModules.Count == 0)
            return;

        var moduleType = compilation.GetTypeByMetadataName("Hexed.Module");
        var useType = compilation.GetTypeByMetadataName("Hexed.Use`1");
        var globType = compilation.GetTypeByMetadataName("Hexed.Glob`1");
        var configureType = compilation.GetTypeByMetadataName("Hexed.Configure`1");

        if (moduleType is null || useType is null || globType is null || configureType is null)
            return;

        // transitively discover all reachable module types
        var allModules = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var queue = new Queue<INamedTypeSymbol>(entryModules);

        while (queue.Count > 0)
        {
            var module = queue.Dequeue();

            if (allModules.ContainsKey(module))
                continue;

            allModules[module] = module;

            foreach (var iface in module.AllInterfaces)
            {
                if (!iface.IsGenericType)
                    continue;

                var definition = iface.OriginalDefinition;

                if (!SymbolEqualityComparer.Default.Equals(definition, useType) &&
                    !SymbolEqualityComparer.Default.Equals(definition, globType) &&
                    !SymbolEqualityComparer.Default.Equals(definition, configureType))
                    continue;

                var argument = iface.TypeArguments[0] as INamedTypeSymbol;

                if (argument is null)
                    continue;

                if (IsModule(argument, moduleType) && !allModules.ContainsKey(argument))
                    queue.Enqueue(argument);
            }
        }

        var useArgs = new List<(INamedTypeSymbol Module, string FieldName, List<INamedTypeSymbol> Args)>();
        var globArgs = new List<(INamedTypeSymbol Module, string FieldName, List<INamedTypeSymbol> Args)>();
        var configureModuleArgs = new List<(INamedTypeSymbol Module, string FieldName, List<INamedTypeSymbol> Args)>();
        var componentArgs = new List<(INamedTypeSymbol Module, string FieldName, List<INamedTypeSymbol> Args)>();

        foreach (var module in allModules.Values)
        {
            var used = GetInterfaceArguments(module, useType)
                .Where(t => IsModule(t, moduleType))
                .ToList();

            if (used.Count > 0)
                useArgs.Add((module, FieldName(module, "used"), used));

            var globbed = GetInterfaceArguments(module, globType)
                .Where(t => IsModule(t, moduleType))
                .ToList();

            if (globbed.Count > 0)
                globArgs.Add((module, FieldName(module, "globbed"), globbed));

            var allConfigured = GetInterfaceArguments(module, configureType).ToList();
            var configuredModules = allConfigured.Where(t => IsModule(t, moduleType)).ToList();

            if (configuredModules.Count > 0)
                configureModuleArgs.Add((module, FieldName(module, "configured"), configuredModules));

            var components = allConfigured.Where(t => !IsModule(t, moduleType)).ToList();

            if (components.Count > 0)
                componentArgs.Add((module, FieldName(module, "components"), components));
        }

        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace Hexed;");
        sb.AppendLine();
        sb.AppendLine("public sealed class GeneratedMetadata : Metadata");
        sb.AppendLine("{");

        // static arrays
        foreach (var (_, fieldName, args) in useArgs)
            sb.AppendLine($"    private static readonly Type[] {fieldName} = [{string.Join(", ", args.Select(t => $"typeof({GlobalType(t)})"))}];");

        foreach (var (_, fieldName, args) in globArgs)
            sb.AppendLine($"    private static readonly Type[] {fieldName} = [{string.Join(", ", args.Select(t => $"typeof({GlobalType(t)})"))}];");

        foreach (var (_, fieldName, args) in configureModuleArgs)
            sb.AppendLine($"    private static readonly Type[] {fieldName} = [{string.Join(", ", args.Select(t => $"typeof({GlobalType(t)})"))}];");

        foreach (var (_, fieldName, args) in componentArgs)
            sb.AppendLine($"    private static readonly Type[] {fieldName} = [{string.Join(", ", args.Select(t => $"typeof({GlobalType(t)})"))}];");

        sb.AppendLine();

        // UsedModules
        sb.AppendLine("    public Type[] UsedModules(Type moduleType)");
        sb.AppendLine("    {");
        foreach (var (module, fieldName, _) in useArgs)
            sb.AppendLine($"        if (moduleType == typeof({GlobalType(module)})) return {fieldName};");
        sb.AppendLine("        return [];");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GlobbedModules
        sb.AppendLine("    public Type[] GlobbedModules(Type moduleType)");
        sb.AppendLine("    {");
        foreach (var (module, fieldName, _) in globArgs)
            sb.AppendLine($"        if (moduleType == typeof({GlobalType(module)})) return {fieldName};");
        sb.AppendLine("        return [];");
        sb.AppendLine("    }");
        sb.AppendLine();

        // ConfiguredModules
        sb.AppendLine("    public Type[] ConfiguredModules(Type moduleType)");
        sb.AppendLine("    {");
        foreach (var (module, fieldName, _) in configureModuleArgs)
            sb.AppendLine($"        if (moduleType == typeof({GlobalType(module)})) return {fieldName};");
        sb.AppendLine("        return [];");
        sb.AppendLine("    }");
        sb.AppendLine();

        // ConfiguredComponents
        sb.AppendLine("    public Type[] ConfiguredComponents(Type moduleType)");
        sb.AppendLine("    {");
        foreach (var (module, fieldName, _) in componentArgs)
            sb.AppendLine($"        if (moduleType == typeof({GlobalType(module)})) return {fieldName};");
        sb.AppendLine("        return [];");
        sb.AppendLine("    }");
        sb.AppendLine();

        // CreateModule
        sb.AppendLine("    public Module CreateModule(Type moduleType)");
        sb.AppendLine("    {");
        foreach (var module in allModules.Values)
        {
            sb.AppendLine(
                $"        if (moduleType == typeof({GlobalType(module)})) return new {GlobalType(module)}();");
        }

        sb.AppendLine("        throw new global::Hexed.HexedException.UnknownModule($\"Unknown module type {moduleType}\");");
        sb.AppendLine("    }");
        sb.AppendLine();

        // InvokeConfigure
        sb.AppendLine("    public void InvokeConfigure(object module, Type configurableType, object dependency)");
        sb.AppendLine("    {");
        foreach (var module in allModules.Values)
        {
            var allConfigured = GetInterfaceArguments(module, configureType).ToList();

            foreach (var configured in allConfigured)
            {
                sb.AppendLine(
                    $"        if (module is {GlobalType(module)} && configurableType == typeof({GlobalType(configured)}))");
                sb.AppendLine(
                    $"        {{ (({GlobalType(module)})module).Configure(({GlobalType(configured)})dependency); return; }}");
            }
        }

        sb.AppendLine(
            "        throw new global::Hexed.HexedException.UnknownConfigureInvocation($\"Unknown configure invocation {module.GetType()} / {configurableType}\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("internal static class HexedInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    internal static void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine("        global::Hexed.Modules.Metadata = new global::Hexed.GeneratedMetadata();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ctx.AddSource("GeneratedMetadata.g.cs", sb.ToString());
    }

    private static bool IsModule(INamedTypeSymbol symbol, INamedTypeSymbol moduleType)
    {
        return symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, moduleType));
    }

    private static IEnumerable<INamedTypeSymbol> GetInterfaceArguments(INamedTypeSymbol symbol, INamedTypeSymbol openGenericType) =>
        symbol.AllInterfaces
            .Where(i => i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, openGenericType))
            .Select(i => i.TypeArguments[0])
            .OfType<INamedTypeSymbol>();

    private static string GlobalType(INamedTypeSymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string FieldName(INamedTypeSymbol symbol, string suffix)
    {
        var name = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("+", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_");
        return $"s_{name}_{suffix}";
    }
}