using Hexed.Text;
using System;
using System.Linq;

namespace Hexed;

public interface Glob<TModule> : Use<TModule> where TModule : class, Module;

internal static class Glob
{
    private static string[]? _hexed;

    internal static string[] Hexed => _hexed ??= (Environment.GetEnvironmentVariable("HEXED") ?? string.Empty)
        .Split(';')
        .Select(glob => glob.Trim())
        .Where(glob => !string.IsNullOrWhiteSpace(glob))
        .ToArray();

    internal static void ResetHexed()
    {
        _hexed = null;
    }

    private static bool MatchesGlob(string name, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(name, regexPattern);
    }

    public static bool IsMatch(Type moduleType)
    {
        if (Hexed.Length == 0)
        {
            return true;
        }

        var name = moduleType.TypeName();
        return Hexed.Any(p => MatchesGlob(name, p));
    }
}