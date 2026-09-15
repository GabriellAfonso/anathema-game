#nullable enable
using Anathema.Net.Facade;
using NUnit.Framework;

namespace Anathema.Client.Scenes.Tests
{
    /// <summary>FR-028, research R9: o mapa estágio → cena.</summary>
    public class SceneRouteTests
    {
        [TestCase(ClientStage.SignedOut, "LoginScene")]
        [TestCase(ClientStage.SignedIn, "HomeScene")]
        [TestCase(ClientStage.Searching, "HomeScene")]
        [TestCase(ClientStage.Paired, "VersusScene")]
        [TestCase(ClientStage.InMatch, "MatchScene")]
        [TestCase(ClientStage.MatchFinished, "MatchScene")]
        public void CadaEstagioTemSuaCena(ClientStage stage, string scene)
        {
            Assert.That(SceneRoute.For(stage), Is.EqualTo(scene));
        }

        [Test]
        public void PartidaIndisponivelMantemACenaAtual()
        {
            Assert.That(SceneRoute.For(ClientStage.MatchUnavailable), Is.Null);
        }
    }
}
