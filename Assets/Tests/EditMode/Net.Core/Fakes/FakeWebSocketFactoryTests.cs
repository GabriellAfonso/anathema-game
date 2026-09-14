#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class FakeWebSocketFactoryTests
    {
        [Test]
        public void CadaCreateDevolveSocketNovoEOcioso()
        {
            FakeWebSocketFactory sockets = new FakeWebSocketFactory();

            IWebSocket first = sockets.Create();
            IWebSocket second = sockets.Create();

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(sockets.Created, Is.EqualTo(new[] { first, second }));
            Assert.That(sockets.Latest, Is.SameAs(second));
            Assert.That(sockets.Latest.OpenedUrl, Is.Null);
        }

        [Test]
        public void LatestSemCriacaoLancaDizendoOQueFaltou()
        {
            FakeWebSocketFactory sockets = new FakeWebSocketFactory();

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => _ = sockets.Latest);

            Assert.That(error.Message, Does.Contain("Create()"));
        }
    }
}
