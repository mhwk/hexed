using AwesomeAssertions;
using System.IO;
using System.Runtime.CompilerServices;

namespace Hexed.Analyzer.Test;

public static class Snapshot
{
    public static void AssertGeneratedSourceMatches(
        string actualContent,
        [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "")
    {
        var snapshotDir = Path.Combine(Path.GetDirectoryName(callerFilePath)!, "Snapshots");
        Directory.CreateDirectory(snapshotDir);
        var snapshotPath = Path.Combine(snapshotDir, $"{callerMemberName}.g.cs");

        var normalized = actualContent.Replace("\r\n", "\n");

        if (!File.Exists(snapshotPath))
        {
            File.WriteAllText(snapshotPath, normalized);
            return;
        }

        var expected = File.ReadAllText(snapshotPath).Replace("\r\n", "\n");
        expected.Should().Be(normalized);
    }
}
