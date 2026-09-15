#nullable enable
using System;
using System.Linq;
using NUnit.Framework;

namespace Anathema.Client.Scenes.Tests
{
    /// <summary>FR-022, SC-006: os tipos públicos da borda de cena (contracts/presentation-surface.md).</summary>
    public class SceneEdgeSurfaceTests
    {
        private static readonly string[] Edge =
        {
            "ClientHost", "ISceneClientConsumer", "SceneClientBinder", "SceneRoute", "SceneRouter", "SceneSubscriptions",
        };

        [Test]
        public void BordaDeCenaExpoeSoOListado()
        {
            string[] exported = typeof(SceneRoute).Assembly.GetExportedTypes().Where(type => !type.IsNested).Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

            Assert.That(exported.Except(Edge), Is.Empty, "tipos públicos fora da lista");
            Assert.That(Edge.Except(exported), Is.Empty, "tipos da lista que não são públicos");
        }
    }
}
