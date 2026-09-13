#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace Anathema.Net.Editor.Tests
{
    public class EnvironmentSelectionReaderTests
    {
        private readonly EditorTestObjects objects = new EditorTestObjects();

        [TearDown]
        public void DestroyObjects() => objects.DestroyAll();

        [TestCase(false, "AppConfig_Dev", "configDev")]
        [TestCase(true, "AppConfig_Prod", "configProd")]
        public void LeOConfigSelecionadoPorIsProd(bool production, string expectedConfig, string expectedField)
        {
            Component selector = objects.Selector(production, objects.Config("AppConfig_Dev", false), objects.Config("AppConfig_Prod", true));

            Assert.That(EnvironmentSelectionReader.TryRead(selector, out EnvironmentSelection? selection), Is.True);
            Assert.That(selection!.Config!.name, Is.EqualTo(expectedConfig));
            Assert.That(selection.SelectedField, Is.EqualTo(expectedField));
            Assert.That(selection.IsProd, Is.EqualTo(production));
        }

        [Test]
        public void ComponenteSemOsCamposEhIgnorado()
        {
            GameObject owner = objects.Owner();

            Assert.That(EnvironmentSelectionReader.TryRead(owner.transform, out EnvironmentSelection? selection), Is.False);
            Assert.That(selection, Is.Null);
        }

        [Test]
        public void ConfigSelecionadoVazioVemNulo()
        {
            Component selector = objects.Selector(true, objects.Config("AppConfig_Dev", false), null);

            Assert.That(EnvironmentSelectionReader.TryRead(selector, out EnvironmentSelection? selection), Is.True);
            Assert.That(selection!.Config, Is.Null);
            Assert.That(selection.SelectedField, Is.EqualTo("configProd"));
        }
    }
}
