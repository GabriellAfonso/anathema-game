#nullable enable
using System.Linq;
using System.Reflection;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>FR-001, FR-022: a fachada é núcleo, sem motor, sem Newtonsoft, sem socket concreto, sem codec nem borda.</summary>
    public class FacadeAssemblyBoundaryTests
    {
        private static Assembly FacadeAssembly => typeof(AnathemaClient).Assembly;

        [Test]
        public void FachadaNaoReferenciaMotorNemNewtonsoft()
        {
            Assert.That(AssemblySignatureScanner.FindReferencesStartingWith(FacadeAssembly, "UnityEngine", "UnityEditor", "Newtonsoft"), Is.Empty);
        }

        [Test]
        public void FachadaNaoConheceCodecNemBorda()
        {
            string[] references = FacadeAssembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToArray();

            Assert.That(references, Has.None.EqualTo("Anathema.Net.Json").And.None.EqualTo("Anathema.Net.Unity"));
        }

        [TestCase("System.Net.WebSockets")]
        [TestCase("UnityEngine")]
        [TestCase("Newtonsoft.Json")]
        public void NenhumTipoDaFachadaUsa(string forbiddenNamespace)
        {
            Assert.That(AssemblySignatureScanner.FindUsages(FacadeAssembly, forbiddenNamespace), Is.Empty);
        }
    }
}
