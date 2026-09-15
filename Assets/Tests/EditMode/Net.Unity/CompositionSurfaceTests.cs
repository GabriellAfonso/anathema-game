#nullable enable
using System;
using System.Linq;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>FR-022, SC-006: da borda Unity só a composição é pública (contracts/composition-and-scenes.md).</summary>
    public class CompositionSurfaceTests
    {
        private static readonly string[] Composition =
        {
            "ClientComposition", "ClientCompositionOptions", "ComposedClient", "NetworkLayerHost", "RefreshTokenVaultSlot", "ServerRoutes",
        };

        [Test]
        public void BordaUnityExpoeSoAComposicao()
        {
            string[] exported = typeof(ClientComposition).Assembly.GetExportedTypes().Where(type => !type.IsNested).Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

            Assert.That(exported.Except(Composition), Is.Empty, "tipos públicos fora da composição");
            Assert.That(Composition.Except(exported), Is.Empty, "tipos da composição que não são públicos");
        }
    }
}
