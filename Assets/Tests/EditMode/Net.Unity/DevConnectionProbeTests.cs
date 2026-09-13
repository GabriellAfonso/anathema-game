#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class DevConnectionProbeTests
    {
        [TestCase(true, "true", true)]
        [TestCase(true, "TRUE", true)]
        [TestCase(true, "false", false)]
        [TestCase(true, null, false)]
        [TestCase(false, "true", false)]
        public void SoRodaEmDesenvolvimentoComParametroTrue(bool developmentBuild, string? probeValue, bool expected)
        {
            Assert.That(DevConnectionProbe.ShouldRun(developmentBuild, probeValue), Is.EqualTo(expected));
        }

        [Test]
        public void BasesSeguemTls()
        {
            Assert.That(DevConnectionProbe.ResolveBases("192.168.0.10:8000", useTls: false), Is.EqualTo(("http://192.168.0.10:8000", "ws://192.168.0.10:8000")));
            Assert.That(DevConnectionProbe.ResolveBases("api.anathema.com", useTls: true), Is.EqualTo(("https://api.anathema.com", "wss://api.anathema.com")));
        }
    }
}
