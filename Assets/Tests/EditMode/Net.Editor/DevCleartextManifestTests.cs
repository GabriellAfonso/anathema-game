#nullable enable
using System;
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace Anathema.Net.Editor.Tests
{
    public class DevCleartextManifestTests
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private string manifestPath = "";

        [SetUp]
        public void WriteGeneratedManifest()
        {
            manifestPath = Path.Combine(Path.GetTempPath(), "anathema-manifest-" + Guid.NewGuid().ToString("N") + ".xml");
            File.WriteAllText(manifestPath,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<manifest xmlns:android=\"" + AndroidNamespace + "\">\n" +
                "  <application android:extractNativeLibs=\"true\" />\n" +
                "</manifest>\n");
        }

        [TearDown]
        public void DeleteManifest() => File.Delete(manifestPath);

        [Test]
        public void CleartextSoEmDesenvolvimento()
        {
            Assert.That(DevCleartextManifest.CleartextValueFor(isDevelopmentBuild: true), Is.True);
            Assert.That(DevCleartextManifest.CleartextValueFor(isDevelopmentBuild: false), Is.False);
        }

        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void GravaOAtributoNoApplicationComONamespaceDoAndroid(bool cleartext, string expected)
        {
            DevCleartextManifest.ApplyTo(manifestPath, cleartext);

            XmlElement application = ReadApplication();
            Assert.That(application.GetAttribute("usesCleartextTraffic", AndroidNamespace), Is.EqualTo(expected));
            Assert.That(application.GetAttribute("extractNativeLibs", AndroidNamespace), Is.EqualTo("true"));
        }

        [Test]
        public void ProducaoDepoisDeDesenvolvimentoSobrescreve()
        {
            DevCleartextManifest.ApplyTo(manifestPath, cleartext: true);

            DevCleartextManifest.ApplyTo(manifestPath, cleartext: false);

            Assert.That(ReadApplication().GetAttribute("usesCleartextTraffic", AndroidNamespace), Is.EqualTo("false"));
        }

        [Test]
        public void ManifestoSemApplicationLanca()
        {
            File.WriteAllText(manifestPath, "<manifest xmlns:android=\"" + AndroidNamespace + "\" />");

            Assert.Throws<InvalidOperationException>(() => DevCleartextManifest.ApplyTo(manifestPath, cleartext: false));
        }

        private XmlElement ReadApplication()
        {
            XmlDocument manifest = new XmlDocument();
            manifest.Load(manifestPath);
            return (XmlElement)manifest.SelectSingleNode("/manifest/application")!;
        }
    }
}
