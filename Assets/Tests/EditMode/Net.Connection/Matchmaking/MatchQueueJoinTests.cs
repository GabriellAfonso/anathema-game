#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US2-1, US2-2 e FR-028.</summary>
    public class MatchQueueJoinTests
    {
        private ConnectionTestRig rig = null!;
        private AuthenticatedConnection connection = null!;
        private MatchQueue queue = null!;

        [SetUp]
        public void CreateQueue()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
            connection = rig.Connection(ConnectionTestRig.Deterministic(maxAttempts: 5));
            queue = rig.QueueOver(connection);
        }

        [Test]
        public void EntrarAbreEMandaJoinQueueComODeck()
        {
            JoinOutcome outcome = queue.Join(new DeckId(4));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Connecting));

            rig.OpenLatest();

            Assert.That(outcome, Is.EqualTo(JoinOutcome.Started));
            Assert.That(rig.JoinTexts().Single(), Is.EqualTo("{\"type\":\"join_queue\",\"payload\":{\"deck_id\":4}}"));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
            Assert.That(queue.SearchDeck, Is.EqualTo(new DeckId(4)));
            Assert.That(rig.LogValue("queue_join_sent", "deck_id"), Is.EqualTo("4"));
        }

        [Test]
        public void MatchFoundEntregaOPareamentoEFechaOSocket()
        {
            List<MatchPairing> pairings = new List<MatchPairing>();
            queue.Paired += pairings.Add;
            queue.Join(new DeckId(4));
            FakeWebSocket socket = rig.OpenLatest();

            rig.Receive(TestFrames.MatchFound);
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(pairings.Single().Match, Is.EqualTo(new MatchId("match-7")));
            Assert.That(pairings.Single().Opponent.Nickname, Is.EqualTo("two"));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Paired));
            Assert.That(queue.SearchDeck, Is.Null);
            Assert.That(socket.CloseRequests, Is.EqualTo(1));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntrarConectandoOuProcurandoEhRecusadoLocalmente()
        {
            queue.Join(new DeckId(4));
            Assert.That(queue.Join(new DeckId(5)), Is.EqualTo(JoinOutcome.AlreadyQueued));

            rig.OpenLatest();

            Assert.That(queue.Join(new DeckId(5)), Is.EqualTo(JoinOutcome.AlreadyQueued));
            Assert.That(rig.JoinTexts().Count, Is.EqualTo(1));
            Assert.That(queue.SearchDeck, Is.EqualTo(new DeckId(4)));
        }

        [Test]
        public void EnvioQueNaoSaiContinuaConectandoEReenviaNaProximaAbertura()
        {
            queue.Join(new DeckId(4));
            rig.Sockets.Latest.NextSendOutcome = SocketSendOutcome.Failed("broken pipe");
            rig.OpenLatest();
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Connecting));

            rig.Sockets.Latest.SimulateClosed(null, "abnormal");
            rig.Advance(TimeSpan.FromSeconds(0.5));
            rig.OpenLatest();

            Assert.That(rig.JoinTexts().Single(), Does.Contain("\"deck_id\":4"));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
        }

        [Test]
        public void EntrarComOSocketJaAbertoMandaNaHora()
        {
            queue.Join(new DeckId(4));
            FakeWebSocket socket = rig.OpenLatest();
            rig.Receive(TestFrames.Refused("deck_not_specified"));

            JoinOutcome outcome = queue.Join(new DeckId(5));

            Assert.That(outcome, Is.EqualTo(JoinOutcome.Started));
            Assert.That(socket.SentTexts.Last(), Does.Contain("\"deck_id\":5"));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntrarDeNovoDepoisDeParearComecaOutraBusca()
        {
            queue.Join(new DeckId(4));
            rig.OpenLatest();
            rig.Receive(TestFrames.MatchFound);

            JoinOutcome outcome = queue.Join(new DeckId(4));

            Assert.That(outcome, Is.EqualTo(JoinOutcome.Started));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
        }
    }
}
