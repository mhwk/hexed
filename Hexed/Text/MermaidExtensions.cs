using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hexed.Text;

public static class MermaidExtensions
{
    extension(Modules modules)
    {
        internal string ToMermaid()
        {
            var sb = new StringBuilder();
            sb.AppendLine("graph TD");

            var indices = new Dictionary<Type, int>();

            foreach (var module in modules)
            {
                var index = indices.Count;
                indices[module.GetType()] = index;
                sb.AppendLine($"    N{index}[\"{module.GetType().FullName}\"]");
            }

            var edges = new HashSet<string>();

            foreach (var module in modules)
            {
                var fromIndex = indices[module.GetType()];
                var deps = Modules.Descriptor.UsedModules(module.GetType())
                    .Concat(Modules.Descriptor.ConfiguredModules(module.GetType()));

                foreach (var depType in deps)
                {
                    if (!indices.TryGetValue(depType, out var toIndex)) continue;
                    var edge = $"    N{fromIndex} --> N{toIndex}";
                    if (edges.Add(edge)) sb.AppendLine(edge);
                }
            }

            return sb.ToString();
        }
    }
}