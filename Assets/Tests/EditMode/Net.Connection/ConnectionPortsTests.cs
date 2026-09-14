#nullable enable
using System;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    public class ConnectionPortsTests
    {
        [TestCase("sockets")]
        [TestCase("clock")]
        [TestCase("ticker")]
        [TestCase("lifecycle")]
        [TestCase("reachability")]
        [TestCase("queue")]
        [TestCase("log")]
        public void PortaNulaLancaNomeandoOCampo(string missing)
        {
            ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Build(missing));

            Assert.That(error.ParamName, Is.EqualTo(missing));
            Assert.That(error.Message, Does.Contain("expected"));
        }

        [Test]
        public void PortasCompletasFicamAcessiveis()
        {
            ConnectionPorts ports = Build(null);

            Assert.That(ports.Sockets, Is.Not.Null);
            Assert.That(ports.Queue, Is.Not.Null);
        }

        private static ConnectionPorts Build(string? missing)
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();
            FakeClientLog log = new FakeClientLog();
            return new ConnectionPorts(
                missing == "sockets" ? null! : new FakeWebSocketFactory(),
                missing == "clock" ? null! : clock,
                missing == "ticker" ? null! : new FakeFrameTicker(),
                missing == "lifecycle" ? null! : new FakeAppLifecycle(clock),
                missing == "reachability" ? null! : new FakeNetworkReachability(NetworkKind.LocalArea),
                missing == "queue" ? null! : new MainThreadQueue(log),
                missing == "log" ? null! : log);
        }
    }
}
