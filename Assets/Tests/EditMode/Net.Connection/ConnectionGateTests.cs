#nullable enable
using System;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US1-5: gate da partida é terminal.</summary>
    public class ConnectionGateTests
    {
        private ConnectionTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
        }

        [TestCase(4400, MatchRefusalDetail.NoMatchId)]
        [TestCase(4403, MatchRefusalDetail.NotAParticipant)]
        [TestCase(4404, MatchRefusalDetail.MatchNotFound)]
        public void MatchDeniedComCodigoDesisteSemRenovarNemTentar(int code, MatchRefusalDetail detail)
        {
            AuthenticatedConnection connection = ConnectMatch();

            FakeWebSocket socket = rig.OpenLatest();
            socket.SimulateText(TestFrames.MatchDenied);
            socket.SimulateClosed(code, "match_denied");
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.GivenUp(GiveUpReason.MatchRefused(detail))));
            Assert.That(rig.Tokens.RenewRequests, Is.EqualTo(0));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(rig.LogValue("connection_gave_up", "match_detail"), Is.EqualTo(detail.ToString()));
        }

        [Test]
        public void MatchDeniedComFechamentoSemCodigoEhRecusaSemDetalhe()
        {
            AuthenticatedConnection connection = ConnectMatch();

            FakeWebSocket socket = rig.OpenLatest();
            socket.SimulateText(TestFrames.MatchDenied);
            socket.SimulateClosed(null, "abnormal");

            Assert.That(connection.Status.GiveUp, Is.EqualTo(GiveUpReason.MatchRefused(MatchRefusalDetail.Unspecified)));
        }

        [Test]
        public void CodigoDeGateSemFrameTambemDesiste()
        {
            AuthenticatedConnection connection = ConnectMatch();

            rig.OpenLatest().SimulateClosed(4404, "");

            Assert.That(connection.Status.GiveUp, Is.EqualTo(GiveUpReason.MatchRefused(MatchRefusalDetail.MatchNotFound)));
        }

        [Test]
        public void MatchDeniedNoSocketDeFilaTambemDesisteERegistra()
        {
            AuthenticatedConnection connection = rig.Connection();
            connection.Connect(ConnectionTestRig.MatchmakingTarget);

            FakeWebSocket socket = rig.OpenLatest();
            socket.SimulateText(TestFrames.MatchDenied);
            socket.SimulateClosed(4404, "match_denied");

            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.GaveUp));
            Assert.That(rig.LogValue("connection_gave_up", "kind"), Is.EqualTo(nameof(GiveUpKind.MatchRefused)));
        }

        [Test]
        public void SocketDePartidaLevaMatchIdEToken()
        {
            ConnectMatch();

            Assert.That(rig.Sockets.Latest.OpenedUrl!.Query, Is.EqualTo("?matchId=match-1&token=token-A"));
        }

        private AuthenticatedConnection ConnectMatch()
        {
            AuthenticatedConnection connection = rig.Connection();
            connection.Connect(ConnectionTestRig.MatchTarget());
            return connection;
        }
    }
}
