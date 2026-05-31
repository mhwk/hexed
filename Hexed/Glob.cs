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

    private static bool MatchesGlob(string name, ReadOnlySpan<char> pattern)
    {
        var nameSpan = name.AsSpan();
        var ni = 0;
        var pi = 0;
        var lastStar = -1;
        var lastMatch = 0;

        while (ni < nameSpan.Length)
        {
            if (pi < pattern.Length && (pattern[pi] == '?' || pattern[pi] == nameSpan[ni]))
            {
                ni++;
                pi++;
            }
            else if (pi < pattern.Length && pattern[pi] == '*')
            {
                lastStar = pi;
                lastMatch = ni;
                pi++;
            }
            else if (lastStar >= 0)
            {
                pi = lastStar + 1;
                lastMatch++;
                ni = lastMatch;
            }
            else
            {
                return false;
            }
        }

        while (pi < pattern.Length && pattern[pi] == '*')
        {
            pi++;
        }

        return pi == pattern.Length;
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