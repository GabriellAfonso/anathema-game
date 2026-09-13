#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class CleartextPolicyTests
    {
        [TestCase("https://api.anathema.com/game/cards/")]
        [TestCase("wss://api.anathema.com/ws/matchmaking/")]
        public void TlsSempreAbreMesmoEmProducao(string url)
        {
            Assert.That(new CleartextPolicy(false).Permits(new Uri(url)), Is.True);
        }

        [TestCase("http://192.168.0.10:8000/game/cards/")]
        [TestCase("ws://192.168.0.10:8000/ws/matchmaking/")]
        public void SemTlsSoAbreQuandoPermitido(string url)
        {
            Assert.That(new CleartextPolicy(true).Permits(new Uri(url)), Is.True);
            Assert.That(new CleartextPolicy(false).Permits(new Uri(url)), Is.False);
        }

        [TestCase("ftp://192.168.0.10/file")]
        [TestCase("file:///C:/token.txt")]
        public void OutroEsquemaNuncaAbre(string url)
        {
            Assert.That(new CleartextPolicy(true).Permits(new Uri(url)), Is.False);
        }

        [Test]
        public void UrlRelativaLanca()
        {
            Assert.Throws<ArgumentException>(() => new CleartextPolicy(true).Permits(new Uri("/ws/matchmaking/", UriKind.Relative)));
        }
    }
}
