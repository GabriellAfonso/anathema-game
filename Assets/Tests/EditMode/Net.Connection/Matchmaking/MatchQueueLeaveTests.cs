#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US2-9 e frames que chegam depois de sair ou de parear.</summary>
    public class MatchQueueLeaveTests
    {
        private ConnectionTestRig rig = null!;
        private AuthenticatedConnection connection = null!;
        private MatchQueue queue = null!;
        private List<QueueRefusal> refused = null!;
        private List<GiveUpReason> left = null!;
        private List<MatchPairing> pairings = null!;

        [SetUp]
        public void CreateQueue()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
            connection = rig.Connection(ConnectionTestRig.Deterministic(maxAttempts: 5));
            queue = rig.QueueOver(connection);
            refused = new List<QueueRefusal>();
            left = new List<GiveUpReason>();
            pairings = new List<MatchPairing>();
            queue.Refused += refused.Add;
            queue.LeftQueue += left.Add;
            queue.Paired += pairings.Add;
        }

        [Test]
        public void SairProcurandoFechaSemAvisoDeFalhaENaoReabre()
        {
            queue.Join(new DeckId(4));
            FakeWebSocket socket = rig.OpenLatest();

            queue.Leave();
            rig.Advance(TimeSpan.FromMinutes(1));

            Assert.That(socket.CloseRequests, Is.EqualTo(1));
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.OutOfQueue));
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
            Assert.That(left, Is.Empty);
            Assert.That(refused, Is.Empty);
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void RecusaDepoisDoPareamentoEhDescartada()
        {
            queue.Join(new DeckId(4));
            FakeWebSocket socket = rig.OpenLatest();
            rig.Receive(TestFrames.MatchFound);

            socket.SimulateText(TestFrames.Refused("deck_not_found"));

            Assert.That(refused, Is.Empty);
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.Paired));
        }

        [Test]
        public void MatchFoundDepoisDeSairEhDescartado()
        {
            queue.Join(new DeckId(4));
            FakeWebSocket socket = rig.OpenLatest();
            queue.Leave();

            socket.SimulateText(TestFrames.MatchFound);

            Assert.That(pairings, Is.Empty);
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.OutOfQueue));
        }

        [Test]
        public void DisposeParaDeOuvirAConexao()
        {
            queue.Join(new DeckId(4));
            queue.Dispose();

            rig.OpenLatest();
            rig.Receive(TestFrames.MatchFound);

            Assert.That(rig.JoinTexts(), Is.Empty);
            Assert.That(pairings, Is.Empty);
        }
    }
}
