#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Account;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US1-1 a US1-4: token pedido antes de abrir, renovação, sessão indisponível.</summary>
    public class ConnectionTokenTests
    {
        private ConnectionTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new ConnectionTestRig();
        }

        [Test]
        public void TokenRecusadoRenovaEReabreComONovo()
        {
            rig.Tokens.EnqueueValid("token-A");
            rig.Tokens.EnqueueRenewed("token-B");
            AuthenticatedConnection connection = rig.Connection();
            List<ConnectionPhase> phases = ConnectionTestRig.RecordPhases(connection);

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            Assert.That(rig.Sockets.Latest.OpenedUrl!.Query, Is.EqualTo("?token=token-A"));
            rig.RefuseLatestToken();
            rig.OpenLatest();
            rig.Receive(TestFrames.Pong);

            Assert.That(rig.Tokens.RenewRequests, Is.EqualTo(1));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
            Assert.That(rig.Sockets.Latest.OpenedUrl!.Query, Is.EqualTo("?token=token-B"));
            Assert.That(phases, Is.EqualTo(new[] { ConnectionPhase.Connecting, ConnectionPhase.Connected, ConnectionPhase.RenewingToken, ConnectionPhase.Connecting, ConnectionPhase.Connected }));
            Assert.That(rig.LogValue("connection_proven", "recovered"), Is.EqualTo("True").IgnoreCase);
        }

        [Test]
        public void TresRecusasSeguidasDesistem()
        {
            rig.Tokens.EnqueueValid("token-A");
            rig.Tokens.EnqueueRenewed("token-B");
            rig.Tokens.EnqueueRenewed("token-C");
            AuthenticatedConnection connection = rig.Connection();

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            for (int refusal = 0; refusal < 3; refusal++)
                rig.RefuseLatestToken();

            Assert.That(connection.Status.GiveUp, Is.EqualTo(GiveUpReason.TokenRefusedRepeatedly(3)));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(3));
            Assert.That(rig.Tokens.RenewRequests, Is.EqualTo(2));
        }

        [Test]
        public void RenovacaoSemRedeEsperaSemContarRecusaEPedeTokenDeNovo()
        {
            rig.Tokens.EnqueueValid("token-A");
            rig.Tokens.EnqueueRenewalUnavailable(RenewalUnavailableReason.Transport);
            rig.Tokens.EnqueueRenewed("token-B");
            AuthenticatedConnection connection = rig.Connection();

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            rig.RefuseLatestToken();
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.WaitingRetry));
            rig.Advance(connection.Status.Wait);
            rig.RefuseLatestToken();

            Assert.That(rig.Tokens.ValidRequests, Is.EqualTo(3));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(3));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connecting), "duas recusas de token não passam do limite de 2");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RenovacaoSemSessaoDesiste(bool expired)
        {
            rig.Tokens.EnqueueValid("token-A");
            if (expired)
                rig.Tokens.EnqueueRenewalExpired();
            else
                rig.Tokens.EnqueueRenewalNoSession();
            AuthenticatedConnection connection = rig.Connection();

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            rig.RefuseLatestToken();
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(connection.Status.GiveUp, Is.EqualTo(expired ? GiveUpReason.SessionExpired() : GiveUpReason.NoSession()));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [TestCase(SessionUnavailableKind.NoSession)]
        [TestCase(SessionUnavailableKind.Expired)]
        public void PedidoDeTokenSemSessaoDesisteSemCriarSocket(SessionUnavailableKind kind)
        {
            rig.Tokens.EnqueueSessionUnavailable(kind);
            AuthenticatedConnection connection = rig.Connection();

            connection.Connect(ConnectionTestRig.MatchmakingTarget);

            Assert.That(connection.Status.GiveUp, Is.EqualTo(kind == SessionUnavailableKind.Expired ? GiveUpReason.SessionExpired() : GiveUpReason.NoSession()));
            Assert.That(rig.Sockets.Created, Is.Empty);
        }

        [Test]
        public void PedidoDeTokenIndisponivelEsperaEDepoisAbre()
        {
            rig.Tokens.EnqueueUnavailable(RenewalUnavailableReason.Transport);
            rig.Tokens.EnqueueValid("token-A");
            AuthenticatedConnection connection = rig.Connection();

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            Assert.That(connection.Status, Is.EqualTo(ConnectionStatus.Waiting(1, TimeSpan.FromSeconds(0.5))));
            Assert.That(rig.Sockets.Created, Is.Empty);
            rig.Advance(TimeSpan.FromSeconds(0.5));

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void RenovacaoQueChegaDepoisDeSairEhDescartada()
        {
            rig.Tokens.EnqueueValid("token-A");
            rig.Tokens.EnqueueRenewed("token-B");
            HeldRenewal held = rig.Tokens.HoldNextRenewal();
            AuthenticatedConnection connection = rig.Connection();

            connection.Connect(ConnectionTestRig.MatchmakingTarget);
            rig.RefuseLatestToken();
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.RenewingToken));
            connection.Leave();
            held.Release();

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
            Assert.That(rig.Log.Single("connection_stale_token_result"), Is.Not.Null);
        }
    }
}
