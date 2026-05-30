using Hexed.Text;
using System;
using System.Linq;

namespace Hexed;

public interface Glob<TModule> : Use<TModule> where TModule : class, Module;

internal sealed class Glob
{
    private string[] Patterns
        => field ??= (Environment.GetEnvironmentVariable("HEXED") ?? string.Empty)
            .Split(';')
            .Select(glob => glob.Trim())
            .Where(glob => !string.IsNullOrWhiteSpace(glob))
            .ToArray();

    private string[] Inclusions
        => field ??= Patterns.Where(p => !p.StartsWith('!')).ToArray();

    private string[] Exclusions
        => field ??= Patterns.Where(p => p.StartsWith('!')).Select(p => p[1..]).ToArray();

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

        if (Exclusions.Any(e => MatchesGlob(name, e)))
        {
            return false;
        }

        if (Inclusions.Length == 0)
        {
            return true;
        }

        return Inclusions.Any(p => MatchesGlob(name, p));
    }
}