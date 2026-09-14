#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US2-7, US2-8 e casos de borda da fila com queda.</summary>
    public class MatchQueueReconnectTests
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
        public void QuedaProcurandoReconectaEReenviaOMesmoDeck()
        {
            Search(new DeckId(4));

            rig.Sockets.Latest.SimulateClosed(null, "abnormal");
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Connecting));
            rig.Advance(TimeSpan.FromSeconds(0.5));
            rig.OpenLatest();

            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(2));
            Assert.That(rig.JoinTexts().Single(), Is.EqualTo("{\"type\":\"join_queue\",\"payload\":{\"deck_id\":4}}"));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
        }

        [Test]
        public void TentativasEsgotadasSaemDaFilaComOMotivo()
        {
            List<GiveUpReason> left = new List<GiveUpReason>();
            queue.LeftQueue += left.Add;
            queue.Join(new DeckId(4));

            for (int drop = 0; drop < 6; drop++)
            {
                rig.Sockets.Latest.SimulateClosed(null, "abnormal");
                rig.Advance(TimeSpan.FromSeconds(10));
            }

            Assert.That(left.Single(), Is.EqualTo(GiveUpReason.AttemptsExhausted(5)));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.OutOfQueue));
            Assert.That(queue.SearchDeck, Is.Null);
            Assert.That(rig.LogValue("queue_left", "kind"), Is.EqualTo(nameof(GiveUpKind.AttemptsExhausted)));
        }

        [Test]
        public void ReenvioRecusadoEntregaARecusaSemGastarTentativa()
        {
            List<QueueRefusal> refused = new List<QueueRefusal>();
            queue.Refused += refused.Add;
            Search(new DeckId(4));
            rig.Sockets.Latest.SimulateClosed(null, "abnormal");
            rig.Advance(TimeSpan.FromSeconds(0.5));
            rig.OpenLatest();

            rig.Receive(TestFrames.DeckNotFound);

            Assert.That(refused.Single().Kind, Is.EqualTo(QueueRefusalKind.DeckNotFound));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.OutOfQueue));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
        }

        [Test]
        public void QuedaForaDaBuscaNaoReconecta()
        {
            Search(new DeckId(4));
            rig.Receive(TestFrames.Refused("deck_not_specified"));

            rig.Sockets.Latest.SimulateClosed(null, "abnormal");
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void MatchFoundInvalidoMantemProcurando()
        {
            Search(new DeckId(4));

            rig.Receive("{\"type\": \"match_found\", \"payload\": {\"match_id\": \"m\"}}");

            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
            Assert.That(rig.Log.Single("connection_frame_invalid"), Is.Not.Null);
        }

        [Test]
        public void TokenRecusadoProcurandoRenovaEReenvia()
        {
            rig.Tokens.EnqueueRenewed("token-B");
            queue.Join(new DeckId(4));

            rig.RefuseLatestToken();
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Connecting));
            rig.OpenLatest();

            Assert.That(rig.JoinTexts().Single(), Does.Contain("\"deck_id\":4"));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
        }

        private void Search(DeckId deck)
        {
            queue.Join(deck);
            rig.OpenLatest();
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Searching));
        }
    }
}
