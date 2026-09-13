#nullable enable
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class ServerHostTests
    {
        [TestCase("192.168.0.10:8000", "192.168.0.10:8000")]
        [TestCase("http://192.168.0.10:8000/", "192.168.0.10:8000")]
        [TestCase("wss://api.anathema.com", "api.anathema.com")]
        [TestCase("  HTTPS://api.anathema.com/  ", "api.anathema.com")]
        public void NormalizaParaHostEPorta(string raw, string expected)
        {
            Assert.That(ServerHost.TryParse(raw, out ServerHost? host, out string problem), Is.True, problem);
            Assert.That(host!.HostAndPort, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        [TestCase("192.168.0.10:8000/api")]
        [TestCase("192.168.0.10:99999")]
        [TestCase("192.168.0.10:")]
        [TestCase(":8000")]
        [TestCase("192.168 .0.10")]
        public void RecusaComValorEFormaEsperada(string? raw)
        {
            Assert.That(ServerHost.TryParse(raw, out ServerHost? host, out string problem), Is.False);
            Assert.That(host, Is.Null);
            Assert.That(problem, Does.Contain($"'{raw}'").And.Contain("host[:port]"));
        }
    }
}
