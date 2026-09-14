#nullable enable
using System.Linq;
using System.Reflection;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>
    /// Fronteira da conexão (FR-047, SC-003; constituição, princípio VI): sem motor, sem Newtonsoft,
    /// sem o socket do .NET e sem conhecer codec nem adaptadores. O noEngineReferences da asmdef não
    /// impede Newtonsoft nem referência a outra asmdef.
    /// </summary>
    public class ConnectionAssemblyBoundaryTests
    {
        private static Assembly ConnectionAssembly => typeof(ConnectionTiming).Assembly;

        [Test]
        public void ConexaoNaoReferenciaMotorNemNewtonsoft()
        {
            Assert.That(AssemblySignatureScanner.FindReferencesStartingWith(ConnectionAssembly, "UnityEngine", "UnityEditor", "Newtonsoft"), Is.Empty);
        }

        [Test]
        public void ConexaoNaoConheceCodecNemAdaptadores()
        {
            string[] references = ConnectionAssembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToArray();

            Assert.That(references, Has.None.EqualTo("Anathema.Net.Json").And.None.EqualTo("Anathema.Net.Unity"));
        }

        [TestCase("System.Net.WebSockets")]
        [TestCase("UnityEngine")]
        [TestCase("Newtonsoft.Json")]
        public void NenhumTipoDaConexaoUsa(string forbiddenNamespace)
        {
            Assert.That(AssemblySignatureScanner.FindUsages(ConnectionAssembly, forbiddenNamespace), Is.Empty);
        }
    }
}
