#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US1-7: saída de propósito nunca reconecta.</summary>
    public class ConnectionLeaveTests
    {
        private ConnectionTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
        }

        [Test]
        public void SairConectandoFechaENuncaReabre()
        {
            AuthenticatedConnection connection = Connect();

            connection.Leave();
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(rig.Sockets.Latest.CloseRequests, Is.EqualTo(1));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void SairConectadoCortaFramesEAvisos()
        {
            AuthenticatedConnection connection = Connect();
            FakeWebSocket socket = rig.OpenLatest();
            List<ServerFrame> frames = new List<ServerFrame>();
            int recovered = 0;
            connection.FrameReceived += frames.Add;
            connection.Recovered += () => recovered++;

            connection.Leave();
            socket.SimulateText(TestFrames.Pong);
            socket.SimulateClosed(1000, "closed_by_client");
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(frames, Is.Empty);
            Assert.That(recovered, Is.EqualTo(0));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void SairEsperandoNaoReabre()
        {
            AuthenticatedConnection connection = Connect();
            rig.OpenLatest().SimulateClosed(null, "abnormal");
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.WaitingRetry));

            connection.Leave();
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
        }

        [Test]
        public void ConectarDepoisDeDesistirRecomecaComPoliticaZerada()
        {
            AuthenticatedConnection connection = rig.Connection();
            connection.Connect(ConnectionTestRig.MatchTarget());
            rig.OpenLatest().SimulateClosed(4404, "match_denied");
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.GaveUp));

            connection.Connect(ConnectionTestRig.MatchTarget());
            rig.OpenLatest().SimulateClosed(null, "abnormal");

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(0.5))));
        }

        [Test]
        public void ConectarComOutroAlvoDuranteATentativaLancaComOsDois()
        {
            AuthenticatedConnection connection = Connect();

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => connection.Connect(ConnectionTestRig.MatchTarget()));

            Assert.That(error.Message, Does.Contain("/ws/matchmaking/").And.Contain("/ws/match/"));
        }

        [Test]
        public void ConectarDeNovoNoMesmoAlvoNaoFazNada()
        {
            AuthenticatedConnection connection = Connect();

            connection.Connect(ConnectionTestRig.MatchmakingTarget);

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(rig.Tokens.ValidRequests, Is.EqualTo(1));
        }

        private AuthenticatedConnection Connect()
        {
            AuthenticatedConnection connection = rig.Connection();
            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            return connection;
        }
    }
}
