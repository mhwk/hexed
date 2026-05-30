using Hexed.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hexed;

public interface Glob<TModule> : Use<TModule> where TModule : class, Module;

internal sealed class Glob
{
    private string[] Patterns => field ??= (Environment.GetEnvironmentVariable("HEXED") ?? string.Empty)
        .Split(';')
        .Select(glob => glob.Trim())
        .Where(glob => !string.IsNullOrWhiteSpace(glob))
        .ToArray();

    private static bool MatchesGlob(string name, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(name, regexPattern);
    }

    public bool IsMatch(Type moduleType)
    {
        if (Patterns.Length == 0)
        {
            return true;
        }

        var name = moduleType.TypeName();
        var inclusions = new List<string>();
        var exclusions = new List<string>();

        foreach (var p in Patterns)
        {
            if (p.StartsWith('!'))
            {
                exclusions.Add(p[1..]);
            }
            else
            {
                inclusions.Add(p);
            }
        }

        if (exclusions.Any(e => MatchesGlob(name, e)))
        {
            return false;
        }

        if (inclusions.Count == 0)
        {
            return true;
        }

        return inclusions.Any(p => MatchesGlob(name, p));
    }
}
