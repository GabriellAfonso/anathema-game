#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>research R8: as portas da fachada, nenhuma nula.</summary>
    public class ClientPortsTests
    {
        [TestCase("http")]
        [TestCase("sockets")]
        [TestCase("clock")]
        [TestCase("ticker")]
        [TestCase("lifecycle")]
        [TestCase("reachability")]
        [TestCase("queue")]
        [TestCase("log")]
        [TestCase("codec")]
        [TestCase("vault")]
        [TestCase("accountRoutes")]
        [TestCase("connectionRoutes")]
        [TestCase("timing")]
        public void PortaNulaLancaComONomeDela(string missing)
        {
            ArgumentNullException thrown = Assert.Throws<ArgumentNullException>(() => Build(missing));

            Assert.That(thrown.ParamName, Is.EqualTo(missing));
        }

        [Test]
        public void GuardaAsPortasRecebidas()
        {
            FakeHttpTransport http = new FakeHttpTransport();
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();

            ClientPorts ports = Build(string.Empty, http, vault);

            Assert.That((ports.Http, ports.Vault), Is.EqualTo(((IHttpTransport)http, (IRefreshTokenVault)vault)));
            Assert.That(ports.ConnectionRoutes.Match.AbsolutePath, Is.EqualTo("/ws/match/"));
        }

        private static ClientPorts Build(string missing, FakeHttpTransport? http = null, FakeRefreshTokenVault? vault = null)
        {
            FakeClientLog log = new FakeClientLog();
            FakeMonotonicClock clock = new FakeMonotonicClock();
            return new ClientPorts(Pick(missing, "http", http ?? new FakeHttpTransport()), Pick(missing, "sockets", new FakeWebSocketFactory()),
                Pick(missing, "clock", clock), Pick(missing, "ticker", new FakeFrameTicker()), Pick(missing, "lifecycle", new FakeAppLifecycle(clock)),
                Pick(missing, "reachability", new FakeNetworkReachability(NetworkKind.LocalArea)), Pick(missing, "queue", new MainThreadQueue(log)),
                Pick(missing, "log", log), Pick(missing, "codec", new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log)),
                Pick(missing, "vault", vault ?? new FakeRefreshTokenVault()), Pick(missing, "accountRoutes", FacadeTestRig.AccountRoutesForTests()),
                Pick(missing, "connectionRoutes", FacadeTestRig.ConnectionRoutesForTests()), Pick(missing, "timing", new AccountTiming()));
        }

        private static T Pick<T>(string missing, string name, T value) where T : class => missing == name ? null! : value;
    }
}
