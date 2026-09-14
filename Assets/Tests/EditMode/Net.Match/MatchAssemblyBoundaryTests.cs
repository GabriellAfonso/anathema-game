#nullable enable
using System.Linq;
using System.Reflection;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>FR-046, SC-004: a partida é núcleo, sem motor, sem Newtonsoft e sem socket concreto.</summary>
    public class MatchAssemblyBoundaryTests
    {
        private static Assembly MatchAssembly => typeof(PlayCommand).Assembly;

        [Test]
        public void PartidaNaoReferenciaMotorNemNewtonsoft()
        {
            Assert.That(AssemblySignatureScanner.FindReferencesStartingWith(MatchAssembly, "UnityEngine", "UnityEditor", "Newtonsoft"), Is.Empty);
        }

        [Test]
        public void PartidaNaoConheceCodecNemAdaptadores()
        {
            string[] references = MatchAssembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToArray();

            Assert.That(references, Has.None.EqualTo("Anathema.Net.Json").And.None.EqualTo("Anathema.Net.Unity"));
        }

        [TestCase("System.Net.WebSockets")]
        [TestCase("UnityEngine")]
        [TestCase("Newtonsoft.Json")]
        public void NenhumTipoDaPartidaUsa(string forbiddenNamespace)
        {
            Assert.That(AssemblySignatureScanner.FindUsages(MatchAssembly, forbiddenNamespace), Is.Empty);
        }
    }
}
