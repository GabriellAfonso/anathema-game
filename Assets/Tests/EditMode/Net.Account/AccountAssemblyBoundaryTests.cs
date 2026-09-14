#nullable enable
using System.Linq;
using System.Reflection;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    /// <summary>
    /// Fronteira da conta (SC-002; constituição, princípio VI): sem motor, sem Newtonsoft, sem o
    /// transporte HTTP do Unity e sem conhecer os adaptadores. O noEngineReferences da asmdef não
    /// impede Newtonsoft nem referência a outra asmdef.
    /// </summary>
    public class AccountAssemblyBoundaryTests
    {
        private static Assembly AccountAssembly => typeof(AccountTiming).Assembly;

        [Test]
        public void ContaNaoReferenciaMotorNemNewtonsoft()
        {
            Assert.That(AssemblySignatureScanner.FindReferencesStartingWith(AccountAssembly, "UnityEngine", "UnityEditor", "Newtonsoft"), Is.Empty);
        }

        [Test]
        public void ContaNaoConheceCodecNemAdaptadores()
        {
            string[] references = AccountAssembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToArray();

            Assert.That(references, Has.None.EqualTo("Anathema.Net.Json").And.None.EqualTo("Anathema.Net.Unity"));
        }

        [TestCase("System.Net.WebSockets")]
        [TestCase("UnityEngine.Networking")]
        public void NenhumTipoDaContaUsa(string forbiddenNamespace)
        {
            Assert.That(AssemblySignatureScanner.FindUsages(AccountAssembly, forbiddenNamespace), Is.Empty);
        }
    }
}
