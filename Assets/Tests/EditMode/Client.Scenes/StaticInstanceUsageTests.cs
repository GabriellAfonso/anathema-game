#nullable enable
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Anathema.Client.Scenes.Tests
{
    /// <summary>
    /// FR-027, SC-005: nenhum singleton mutável em Assets/Scripts. Valor imutável (<c>static X Instance { get; } = new X()</c>)
    /// continua permitido; o que sai é o <c>Instance</c> com <c>private set</c> e o campo estático de instância.
    /// </summary>
    public class StaticInstanceUsageTests
    {
        private static readonly Regex MutableProperty = new Regex(@"static\s+[\w.<>]+\??\s+Instance\s*\{\s*get;\s*private\s+set;", RegexOptions.Compiled);
        private static readonly Regex InstanceField = new Regex(@"static\s+[\w.<>]+\??\s+instance\s*;", RegexOptions.Compiled);

        [Test]
        public void NenhumSingletonMutavelNosScripts()
        {
            string scripts = SceneManagerUsageTests.ScriptsRoot();

            string[] offenders = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
                .Where(path => IsSingleton(File.ReadAllText(path)))
                .Select(path => SceneManagerUsageTests.RelativeTo(scripts, path))
                .ToArray();

            Assert.That(offenders, Is.Empty, "arquivos com static Instance mutável");
        }

        [TestCase("public static PlayerSession Instance { get; private set; } = null!;", true)]
        [TestCase("private static ReconnectOverlay instance;", true)]
        [TestCase("public static DeckDeleted Instance { get; } = new DeckDeleted();", false)]
        public void ReconheceOPadraoProibido(string line, bool expected)
        {
            Assert.That(IsSingleton(line), Is.EqualTo(expected));
        }

        private static bool IsSingleton(string text) => MutableProperty.IsMatch(text) || InstanceField.IsMatch(text);
    }
}
