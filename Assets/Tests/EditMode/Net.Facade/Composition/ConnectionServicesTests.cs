#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    public class ConnectionServicesTests
    {
        private FakeWebSocketFactory sockets = null!;
        private FakeMonotonicClock clock = null!;
        private FakeFrameTicker ticker = null!;
        private FakeAccessTokenSource tokens = null!;
        private ConnectionServices services = null!;

        [SetUp]
        public void Compose()
        {
            sockets = new FakeWebSocketFactory();
            clock = new FakeMonotonicClock();
            ticker = new FakeFrameTicker();
            tokens = new FakeAccessTokenSource(clock);
            FakeClientLog log = new FakeClientLog();
            ClientPorts ports = new ClientPorts(new FakeHttpTransport(), sockets, clock, ticker, new FakeAppLifecycle(clock), new FakeNetworkReachability(NetworkKind.LocalArea),
                new MainThreadQueue(log), log, new NewtonsoftProtocolCodec(ConnectionFrames.CreateUnion(), log), new FakeRefreshTokenVault(),
                FacadeTestRig.AccountRoutesForTests(), FacadeTestRig.ConnectionRoutesForTests(), new AccountTiming());
            services = new ConnectionServices(ports, tokens);
        }

        [Test]
        public void FilaEPartidaTemConexoesProprias()
        {
            Assert.That(services.MatchmakingConnection, Is.Not.SameAs(services.MatchConnection));
            Assert.That(services.Routes.Match.AbsolutePath, Is.EqualTo("/ws/match/"));
        }

        [Test]
        public void FilaAbreNaRotaDeFilaPelaConexaoDeFila()
        {
            tokens.EnqueueValid("token-A");

            services.Queue.Join(new DeckId(4));

            Assert.That(sockets.Latest.OpenedUrl!.AbsolutePath, Is.EqualTo("/ws/matchmaking/"));
            Assert.That(services.MatchmakingConnection.Status.Phase, Is.EqualTo(ConnectionPhase.Connecting));
            Assert.That(services.MatchConnection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
        }

        [Test]
        public void DisposeParaDeReconectar()
        {
            tokens.EnqueueValid("token-A");
            services.Queue.Join(new DeckId(4));
            sockets.Latest.SimulateOpened();
            sockets.Latest.SimulateClosed(null, "abnormal");

            services.Dispose();
            clock.Advance(TimeSpan.FromMinutes(1));
            ticker.Tick();

            Assert.That(sockets.Created.Count, Is.EqualTo(1));
        }
    }
}
