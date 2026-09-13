#nullable enable
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Anathema.Net.Editor.Tests
{
    public class ReleaseBuildTlsGateTests
    {
        private const string Scene = "Assets/Scenes/BootstrapScene.unity";
        private readonly EditorTestObjects objects = new EditorTestObjects();

        [TearDown]
        public void DestroyObjects() => objects.DestroyAll();

        [Test]
        public void SoValidaBuildDeVerdadeSemDevelopment()
        {
            Assert.That(ReleaseBuildTlsGate.ShouldValidate(hasReport: false, BuildOptions.None), Is.False);
            Assert.That(ReleaseBuildTlsGate.ShouldValidate(hasReport: true, BuildOptions.Development), Is.False);
            Assert.That(ReleaseBuildTlsGate.ShouldValidate(hasReport: true, BuildOptions.None), Is.True);
        }

        [Test]
        public void ConfigSemTlsFalhaOBuildComAMensagem()
        {
            Component selector = objects.Selector(false, objects.Config("AppConfig_Dev", false), objects.Config("AppConfig_Prod", true));

            BuildFailedException failure = Assert.Throws<BuildFailedException>(() => ReleaseBuildTlsGate.Validate(Scene, new[] { selector }));

            Assert.That(failure.Message, Does.Contain("AppConfig_Dev").And.Contain("useTls=false").And.Contain(Scene));
        }

        [Test]
        public void ConfigComTlsPassa()
        {
            Component selector = objects.Selector(true, objects.Config("AppConfig_Dev", false), objects.Config("AppConfig_Prod", true));

            Assert.DoesNotThrow(() => ReleaseBuildTlsGate.Validate(Scene, new[] { selector }));
        }

        [Test]
        public void ConfigSelecionadoVazioFalha()
        {
            Component selector = objects.Selector(true, objects.Config("AppConfig_Dev", false), null);

            BuildFailedException failure = Assert.Throws<BuildFailedException>(() => ReleaseBuildTlsGate.Validate(Scene, new[] { selector }));

            Assert.That(failure.Message, Does.Contain("configProd"));
        }

        [Test]
        public void ComponenteQueNaoEhSeletorEhIgnorado()
        {
            Component transform = objects.Owner().transform;

            Assert.DoesNotThrow(() => ReleaseBuildTlsGate.Validate(Scene, new[] { transform }));
        }
    }
}
