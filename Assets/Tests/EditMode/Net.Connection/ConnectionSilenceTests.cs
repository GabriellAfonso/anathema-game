#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US3-1 a US3-6: ping, latência, silêncio real e pausa perdoada.</summary>
    public class ConnectionSilenceTests
    {
        private ConnectionTestRig rig = null!;
        private AuthenticatedConnection connection = null!;
        private List<TimeSpan> latencies = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
            latencies = new List<TimeSpan>();
        }

        [Test]
        public void PingSaiNaAberturaEACadaIntervalo()
        {
            FakeWebSocket socket = Open();

            Assert.That(PingsSent(socket), Is.EqualTo(1));
            rig.AdvanceInSeconds(TimeSpan.FromSeconds(9));
            Assert.That(PingsSent(socket), Is.EqualTo(1));
            rig.AdvanceInSeconds(TimeSpan.FromSeconds(1));
            Assert.That(PingsSent(socket), Is.EqualTo(2));
        }

        [Test]
        public void PongCasadoExpoeALatencia()
        {
            FakeWebSocket socket = Open();
            Assert.That(socket.SentTexts[0], Does.Contain("\"sent_at_ms\":").And.Contain("\"ping_seq\":1"));

            rig.Clock.Advance(TimeSpan.FromMilliseconds(80));
            rig.Receive(TestFrames.PongFor(socket.SentTexts[0]));

            Assert.That(latencies, Is.EqualTo(new[] { TimeSpan.FromMilliseconds(80) }));
            Assert.That(connection.LastLatency, Is.EqualTo(TimeSpan.FromMilliseconds(80)));
        }

        [Test]
        public void PongSemMarcadorProvaSemMedirLatencia()
        {
            Open();

            rig.Receive(TestFrames.Pong);

            Assert.That(latencies, Is.Empty);
            Assert.That(rig.Log.Single("connection_proven"), Is.Not.Null);
        }

        [Test]
        public void SilencioRealDepoisDoPongDerrubaEReconecta()
        {
            FakeWebSocket socket = Open();
            rig.Receive(TestFrames.Pong);

            rig.AdvanceInSeconds(TimeSpan.FromSeconds(31));
            rig.Drain();

            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(0.5))));
            Assert.That(socket.CloseRequests, Is.EqualTo(1));
            Assert.That(rig.Log.Single("connection_silence_confirmed"), Is.Not.Null);
            rig.Advance(TimeSpan.FromSeconds(0.5));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
        }

        [Test]
        public void SemPongNuncaDerruba()
        {
            Open();

            rig.AdvanceInSeconds(TimeSpan.FromSeconds(60));
            rig.Drain();

            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void SaltoDeSessentaSegundosComFrameNaFilaNaoDerruba()
        {
            FakeWebSocket socket = Open();
            rig.Receive(TestFrames.Pong);

            rig.Queue.Enqueue(() => rig.Receive(TestFrames.Unknown("match_update")));
            rig.Advance(TimeSpan.FromSeconds(60));
            rig.Drain();

            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(PingsSent(socket), Is.EqualTo(2), "depois da pausa sai um ping para confirmar");
        }

        [Test]
        public void ConfirmacaoAtrasDeUmFrameQueJaEstavaNaFilaNaoDerruba()
        {
            Open();
            rig.Receive(TestFrames.Pong);
            rig.AdvanceInSeconds(TimeSpan.FromSeconds(29));

            rig.Queue.Enqueue(() => rig.Receive(TestFrames.Unknown("match_update")));
            rig.AdvanceInSeconds(TimeSpan.FromSeconds(2));
            rig.Drain();

            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void SocketQueSoRecebePongContaComoAutenticado()
        {
            ConnectionSettings settings = ConnectionTestRig.Deterministic();
            rig.Tokens.EnqueueRenewed("token-B");
            rig.Tokens.EnqueueRenewed("token-C");
            rig.Tokens.EnqueueRenewed("token-D");
            connection = rig.Connection(settings);
            connection.Connect(ConnectionTestRig.MatchmakingTarget);

            rig.RefuseLatestToken();
            rig.OpenLatest();
            rig.Receive(TestFrames.Pong);
            Assert.That(settings.Policy.Attempt, Is.EqualTo(0));
            rig.Sockets.Latest.SimulateText(TestFrames.AuthDenied);
            rig.Sockets.Latest.SimulateClosed(4001, "auth_denied");
            rig.RefuseLatestToken();

            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connecting), "a prova pelo pong zerou a primeira recusa");
        }

        [Test]
        public void VoltaDoSegundoPlanoZeraOSilencioEPingaNaHora()
        {
            FakeWebSocket socket = Open();
            rig.Receive(TestFrames.Pong);
            rig.AdvanceInSeconds(TimeSpan.FromSeconds(20));
            int pingsBefore = PingsSent(socket);

            rig.Lifecycle.SimulateBackground();
            rig.Clock.Advance(TimeSpan.FromMinutes(7));
            rig.Lifecycle.SimulateForeground();
            Assert.That(PingsSent(socket), Is.EqualTo(pingsBefore + 1));
            rig.AdvanceInSeconds(TimeSpan.FromSeconds(29));
            rig.Drain();
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));

            rig.AdvanceInSeconds(TimeSpan.FromSeconds(2));
            rig.Drain();
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.WaitingRetry));
        }

        private FakeWebSocket Open()
        {
            connection = rig.Connection();
            connection.LatencyMeasured += latencies.Add;
            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            return rig.OpenLatest();
        }

        private static int PingsSent(FakeWebSocket socket) => socket.SentTexts.Count(text => text.Contains("\"type\":\"ping\""));
    }
}
