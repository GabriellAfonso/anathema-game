#nullable enable
using System;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US4-1 a US4-5 e casos de borda de segundo plano e rede.</summary>
    public class ConnectionSuspensionTests
    {
        private ConnectionTestRig rig = null!;
        private ConnectionSettings settings = null!;
        private AuthenticatedConnection connection = null!;

        [SetUp]
        public void CreateConnection()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
            settings = ConnectionTestRig.Deterministic();
            connection = rig.Connection(settings);
        }

        [Test]
        public void SegundoPlanoSuspendeATentativaSemGastarLimite()
        {
            ConnectAndProve();
            rig.Sockets.Latest.SimulateClosed(null, "abnormal");

            rig.Lifecycle.SimulateBackground();
            rig.Advance(TimeSpan.FromMinutes(10));

            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.SuspendedBy(SuspensionReason.Background)));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(settings.Policy.Attempt, Is.EqualTo(1));
            Assert.That(rig.LogValue("connection_suspended", "reason"), Is.EqualTo(nameof(SuspensionReason.Background)));
        }

        [Test]
        public void VoltaAoPrimeiroPlanoReabreNaHoraComBackoffZerado()
        {
            ConnectAndProve();
            rig.Sockets.Latest.SimulateClosed(null, "abnormal");
            rig.Lifecycle.SimulateBackground();
            rig.Advance(TimeSpan.FromMinutes(10));

            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
            Assert.That(rig.Tokens.ValidRequests, Is.EqualTo(2));
            Assert.That(rig.LogValue("connection_reopening", "cause"), Is.EqualTo("Foreground"));
            rig.OpenLatest().SimulateClosed(null, "abnormal");
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(0.5))));
        }

        [Test]
        public void VoltaComSocketAbertoSoPingaESegundoPlanoNaoFecha()
        {
            FakeWebSocket socket = ConnectAndProve();
            int pings = socket.SentTexts.Count;

            rig.Lifecycle.SimulateBackground();
            Assert.That(socket.CloseRequests, Is.EqualTo(0));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(socket.SentTexts.Count, Is.EqualTo(pings + 1));
        }

        [Test]
        public void TrocaDeWiFiParaDadosReciclaOSocketNaHora()
        {
            FakeWebSocket old = ConnectAndProve();

            rig.Reachability.SimulateKind(NetworkKind.CarrierData);
            old.SimulateClosed(1000, "closed_by_client");

            Assert.That(old.CloseRequests, Is.EqualTo(1));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connecting));
            Assert.That(settings.Policy.Attempt, Is.EqualTo(0));
            Assert.That(rig.LogValue("connection_socket_recycled", "current"), Is.EqualTo(nameof(NetworkKind.CarrierData)));
        }

        [Test]
        public void SemRedeNaoTentaEVoltaQuandoARedeVolta()
        {
            ConnectAndProve();
            rig.Reachability.SimulateKind(NetworkKind.None);
            rig.Sockets.Latest.SimulateClosed(null, "abnormal");

            rig.Advance(TimeSpan.FromMinutes(10));
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.SuspendedBy(SuspensionReason.NoNetwork)));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            rig.Reachability.SimulateKind(NetworkKind.LocalArea);

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
        }

        [Test]
        public void ConectarSemRedeSuspendeSemPedirToken()
        {
            rig.Reachability.SimulateKind(NetworkKind.None);

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.SuspendedBy(SuspensionReason.NoNetwork)));
            Assert.That(rig.Tokens.ValidRequests, Is.EqualTo(0));
            rig.Reachability.SimulateKind(NetworkKind.CarrierData);

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void WiFiSemRedeDadosReciclaUmaVezSo()
        {
            ConnectAndProve();

            rig.Reachability.SimulateKind(NetworkKind.None);
            rig.Reachability.SimulateKind(NetworkKind.CarrierData);

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
            Assert.That(rig.Log.Entries.Count(entry => entry.EventName == "connection_socket_recycled"), Is.EqualTo(1));
        }

        [Test]
        public void TrocaDeRedeEmSegundoPlanoSoAbreNaVolta()
        {
            ConnectAndProve();

            rig.Lifecycle.SimulateBackground();
            rig.Reachability.SimulateKind(NetworkKind.CarrierData);
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
        }

        [Test]
        public void ConexaoParadaOuDesistidaNaoReageAVoltaNemARede()
        {
            ConnectAndProve();
            connection.Leave();

            rig.Lifecycle.SimulateBackground();
            rig.Lifecycle.SimulateForeground();
            rig.Reachability.SimulateKind(NetworkKind.CarrierData);

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
        }

        [Test]
        public void FilaProcurandoEmSegundoPlanoVoltaAProcurarNaVolta()
        {
            MatchQueue queue = rig.QueueOver(connection);
            queue.Join(new DeckId(4));
            rig.OpenLatest();
            rig.Receive(TestFrames.Pong);

            rig.Lifecycle.SimulateBackground();
            rig.Sockets.Latest.SimulateClosed(null, "abnormal");
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Connecting));
            rig.Clock.Advance(TimeSpan.FromMinutes(6));
            rig.Lifecycle.SimulateForeground();
            rig.OpenLatest();

            Assert.That(rig.JoinTexts().Single(), Does.Contain("\"deck_id\":4"));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
        }

        private FakeWebSocket ConnectAndProve()
        {
            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            FakeWebSocket socket = rig.OpenLatest();
            rig.Receive(TestFrames.Pong);
            return socket;
        }
    }
}
