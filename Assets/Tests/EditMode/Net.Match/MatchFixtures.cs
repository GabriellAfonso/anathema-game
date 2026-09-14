#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Anathema.Net.Match.Tests
{
    /// <summary>
    /// Lê as fixtures de <c>Fixtures/</c>, ao lado deste arquivo. O caminho vem do compilador
    /// (<see cref="CallerFilePathAttribute"/>), e não de <c>Application.dataPath</c>: assim a leitura não
    /// depende do motor e roda igual no editor e na verificação offline.
    /// </summary>
    internal static class MatchFixtures
    {
        internal static string Text(string name)
        {
            string path = Path.Combine(Directory(), name);
            if (!File.Exists(path))
                throw new FileNotFoundException($"fixture '{name}' not found at '{path}': expected a .json file in Assets/Tests/EditMode/Net.Match/Fixtures", path);

            return File.ReadAllText(path);
        }

        internal static string[] Names(string prefix)
        {
            string directory = Directory();
            if (!System.IO.Directory.Exists(directory))
                return Array.Empty<string>();

            return System.IO.Directory.GetFiles(directory, prefix + "*.json").Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray()!;
        }

        private static string Directory([CallerFilePath] string sourcePath = "")
        {
            return Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, "Fixtures");
        }
    }
}
