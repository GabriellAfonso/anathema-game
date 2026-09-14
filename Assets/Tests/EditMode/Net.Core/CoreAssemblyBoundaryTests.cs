#nullable enable
using System.Reflection;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    /// <summary>
    /// Fronteira do núcleo (SC-002; constituição, princípio VI). A asmdef já tem
    /// noEngineReferences, mas isso não impede Newtonsoft nem System.Net.WebSockets.
    /// </summary>
    public class CoreAssemblyBoundaryTests
    {
        private static readonly string[] ForbiddenAssemblyPrefixes = { "UnityEngine", "UnityEditor", "Newtonsoft" };

        private static Assembly CoreAssembly => typeof(MonotonicInstant).Assembly;

        [Test]
        public void NucleoNaoReferenciaMotorNemNewtonsoft()
        {
            Assert.That(AssemblySignatureScanner.FindReferencesStartingWith(CoreAssembly, ForbiddenAssemblyPrefixes), Is.Empty);
        }

        [Test]
        public void NenhumTipoDoNucleoUsaWebSocketsDoDotNet()
        {
            Assert.That(AssemblySignatureScanner.FindUsages(CoreAssembly, "System.Net.WebSockets"), Is.Empty);
        }
    }
}
