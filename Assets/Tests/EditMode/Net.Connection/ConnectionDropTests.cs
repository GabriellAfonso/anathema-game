#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US1-6: queda comum, backoff, prova e volta.</summary>
    public class ConnectionDropTests
    {
        private ConnectionTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
        }

        [Test]
        public void QuedasSeguidasCrescemAEsperaEAvisamAVoltaUmaVez()
        {
            AuthenticatedConnection connection = ConnectAndProve();
            int recovered = 0;
            connection.Recovered += () => recovered++;

            DropLatest();
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(0.5))));
            rig.Advance(TimeSpan.FromSeconds(0.4));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1), "abriu antes da espera");
            rig.Advance(TimeSpan.FromSeconds(0.1));
            DropLatest();
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(2, TimeSpan.FromSeconds(1))));
            rig.Advance(TimeSpan.FromSeconds(1));
            DropLatest();
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(3, TimeSpan.FromSeconds(2))));
            rig.Advance(TimeSpan.FromSeconds(2));
            rig.OpenLatest();
            Assert.That(recovered, Is.EqualTo(0), "abrir não prova a sessão");
            rig.Receive(TestFrames.Pong);

            Assert.That(recovered, Is.EqualTo(1));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(4));
        }

        [Test]
        public void PrimeiraConexaoNaoAvisaQueVoltou()
        {
            int recovered = 0;
            AuthenticatedConnection connection = rig.Connection();
            connection.Recovered += () => recovered++;

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            rig.OpenLatest();
            rig.Receive(TestFrames.Pong);

            Assert.That(recovered, Is.EqualTo(0));
            Assert.That(rig.LogValue("connection_proven", "recovered"), Is.EqualTo("False").IgnoreCase);
        }

        [TestCase(1000)]
        [TestCase(1001)]
        public void FechamentoLimpoOuDeployDoServidorReconecta(int code)
        {
            AuthenticatedConnection connection = ConnectAndProve();

            rig.Sockets.Latest.SimulateClosed(code, "server");

            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(0.5))));
            Assert.That(rig.LogValue("connection_waiting_retry", "close_code"), Is.EqualTo(code.ToString()));
        }

        [Test]
        public void SocketQueCaiAntesDaProvaNaoZeraATentativa()
        {
            AuthenticatedConnection connection = ConnectAndProve();

            DropLatest();
            rig.Advance(TimeSpan.FromSeconds(0.5));
            rig.OpenLatest();
            DropLatest();

            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(2, TimeSpan.FromSeconds(1))));
        }

        [Test]
        public void ProvaZeraATentativa()
        {
            AuthenticatedConnection connection = ConnectAndProve();

            DropLatest();
            rig.Advance(TimeSpan.FromSeconds(0.5));
            rig.OpenLatest();
            rig.Receive(TestFrames.Pong);
            DropLatest();

            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(0.5))));
        }

        [Test]
        public void SeisQuedasSemProvaEsgotamAsCincoTentativasDaFila()
        {
            AuthenticatedConnection connection = rig.Connection(ConnectionTestRig.Deterministic(maxAttempts: 5));

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            for (int drop = 0; drop < 6; drop++)
            {
                DropLatest();
                rig.Advance(TimeSpan.FromSeconds(10));
            }

            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.GivenUp(GiveUpReason.AttemptsExhausted(5))));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(6));
        }

        [Test]
        public void ErroEFechamentoDoMesmoSocketAgendamUmaTentativaSo()
        {
            ConnectAndProve();

            FakeWebSocket socket = rig.Sockets.Latest;
            socket.SimulateError("connection reset");
            socket.SimulateClosed(null, "abnormal");
            rig.Advance(TimeSpan.FromSeconds(5));

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
        }

        private AuthenticatedConnection ConnectAndProve()
        {
            AuthenticatedConnection connection = rig.Connection();
            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            rig.OpenLatest();
            rig.Receive(TestFrames.Pong);
            return connection;
        }

        private void DropLatest() => rig.Sockets.Latest.SimulateClosed(null, "abnormal");
    }
}
