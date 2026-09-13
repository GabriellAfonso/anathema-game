#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class ReleaseTlsRuleTests
    {
        private const string Scene = "Assets/Scenes/BootstrapScene.unity";

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void SoProducaoSemTlsViola(bool developmentBuild, bool useTls)
        {
            Assert.That(ReleaseTlsRule.Check(developmentBuild, Scene, "AppConfig_Dev", false, useTls), Is.Null);
        }

        [Test]
        public void ProducaoSemTlsDizCenaConfigAmbienteECorrecao()
        {
            string? violation = ReleaseTlsRule.Check(false, Scene, "AppConfig_Dev", false, false);

            Assert.That(violation, Does.Contain(Scene)
                .And.Contain("'AppConfig_Dev'")
                .And.Contain("isProd=false")
                .And.Contain("useTls=false")
                .And.Contain("Development Build"));
        }

        [Test]
        public void ConfigVazioDizOCampo()
        {
            Assert.That(ReleaseTlsRule.MissingConfig(Scene, "configProd"), Does.Contain(Scene).And.Contain("configProd"));
        }
    }
}
