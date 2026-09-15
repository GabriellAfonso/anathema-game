#nullable enable
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Anathema.Client.Scenes.Tests
{
    /// <summary>FR-029, SC-005: troca de cena só pelo roteador e pelo binder da borda.</summary>
    public class SceneManagerUsageTests
    {
        private static readonly Regex Usage = new Regex(@"\bSceneManager\s*\.", RegexOptions.Compiled);
        private static readonly string[] Allowed = { "Client/Scenes/SceneRouter.cs", "Client/Scenes/SceneClientBinder.cs" };

        [Test]
        public void SceneManagerSoNoRoteadorENoBinder()
        {
            string scripts = ScriptsRoot();

            string[] offenders = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
                .Where(path => Usage.IsMatch(File.ReadAllText(path)))
                .Select(path => RelativeTo(scripts, path))
                .Where(relative => !Allowed.Contains(relative))
                .ToArray();

            Assert.That(offenders, Is.Empty, "arquivos que carregam cena fora da borda");
        }

        internal static string ScriptsRoot([CallerFilePath] string sourcePath = "")
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, "..", "..", "..", "Scripts"));
        }

        internal static string RelativeTo(string root, string path)
        {
            return path.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
        }
    }
}
