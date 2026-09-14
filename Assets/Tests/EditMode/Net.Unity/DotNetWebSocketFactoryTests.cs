#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class DotNetWebSocketFactoryTests
    {
        [Test]
        public void CadaCreateEhUmSocketRealNovo()
        {
            DotNetWebSocketFactory sockets = new DotNetWebSocketFactory(new MainThreadQueue(new FakeClientLog()), new CleartextPolicy(true), new FakeClientLog());

            IWebSocket first = sockets.Create();
            IWebSocket second = sockets.Create();

            Assert.That(first, Is.InstanceOf<DotNetWebSocket>());
            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void DependenciaNulaLancaNomeandoOCampo()
        {
            FakeClientLog log = new FakeClientLog();
            MainThreadQueue queue = new MainThreadQueue(log);

            Assert.That(Assert.Throws<ArgumentNullException>(() => new DotNetWebSocketFactory(null!, new CleartextPolicy(true), log)).ParamName, Is.EqualTo("queue"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => new DotNetWebSocketFactory(queue, null!, log)).ParamName, Is.EqualTo("policy"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => new DotNetWebSocketFactory(queue, new CleartextPolicy(true), null!)).ParamName, Is.EqualTo("log"));
        }
    }
}
