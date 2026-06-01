using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hexed.Text;

public static class MermaidExtensions
{
    extension(Modules modules)
    {
        public string ToMermaid()
        {
            var sb = new StringBuilder();
            sb.AppendLine("graph TD");

            var indices = new Dictionary<Type, int>();

            foreach (var module in modules)
            {
                var index = indices.Count;
                indices[module.GetType()] = index;
                sb.AppendLine($"    N{index}[\"{module.GetType().TypeName()}\"]");
            }

            var edges = new HashSet<string>();

            foreach (var module in modules)
            {
                var metadata = Modules.Metadata[module.GetType()];
                var fromIndex = indices[module.GetType()];
                var deps = metadata.UsedModules
                    .Concat(metadata.ConfiguredModules);

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