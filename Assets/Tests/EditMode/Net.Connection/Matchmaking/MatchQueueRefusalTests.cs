#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Spec US2-3 a US2-6: recusa volta para fora da fila com o socket aberto.</summary>
    public class MatchQueueRefusalTests
    {
        private ConnectionTestRig rig = null!;
        private AuthenticatedConnection connection = null!;
        private MatchQueue queue = null!;
        private List<QueueRefusal> refused = null!;
        private List<string> failures = null!;

        [SetUp]
        public void CreateQueue()
        {
            rig = new ConnectionTestRig();
            rig.Tokens.EnqueueValid("token-A");
            connection = rig.Connection(ConnectionTestRig.Deterministic(maxAttempts: 5));
            queue = rig.QueueOver(connection);
            refused = new List<QueueRefusal>();
            failures = new List<string>();
            queue.Refused += refused.Add;
            queue.MatchmakingFailed += failures.Add;
            queue.Join(new DeckId(4));
            rig.OpenLatest();
        }

        [TestCase("deck_not_specified", QueueRefusalKind.DeckNotSpecified)]
        [TestCase("malformed_message", QueueRefusalKind.MalformedMessage)]
        [TestCase("unknown_message_type", QueueRefusalKind.UnknownMessageType)]
        [TestCase("brand_new_code", QueueRefusalKind.Unrecognized)]
        public void RecusaTipadaVoltaParaForaDaFilaComSocketAberto(string code, QueueRefusalKind kind)
        {
            rig.Receive(TestFrames.Refused(code));

            Assert.That(refused.Single().Kind, Is.EqualTo(kind));
            Assert.That(refused.Single().Code, Is.EqualTo(code));
            AssertOutOfQueueWithSocketOpen();
        }

        [Test]
        public void DeckNaoEncontradoEcoaODeckEOMesmoSocketAceitaOutroJoin()
        {
            rig.Receive(TestFrames.DeckNotFound);
            queue.Join(new DeckId(5));

            Assert.That(refused.Single().Deck, Is.EqualTo(new DeckId(4)));
            Assert.That(rig.JoinTexts().Count, Is.EqualTo(2));
            Assert.That(rig.Sockets.Created.Count, Is.EqualTo(1));
        }

        [Test]
        public void DeckInvalidoTrazOsDoisProblemas()
        {
            rig.Receive(TestFrames.InvalidDeck);

            Assert.That(refused.Single().Problems.Count, Is.EqualTo(2));
            Assert.That(refused.Single().Problems[0], Is.TypeOf<WrongDeckSize>());
            AssertOutOfQueueWithSocketOpen();
        }

        [Test]
        public void FalhaDePareamentoEhAvisoSeparado()
        {
            rig.Receive(TestFrames.MatchmakingFailed);

            Assert.That(failures.Single(), Is.EqualTo("no profile for one of the paired users"));
            Assert.That(refused, Is.Empty);
            AssertOutOfQueueWithSocketOpen();
        }

        [Test]
        public void RecusaForaDeBuscaEhRegistradaENaoAvisada()
        {
            rig.Receive(TestFrames.Refused("deck_not_specified"));

            rig.Receive(TestFrames.Refused("malformed_message"));

            Assert.That(refused.Count, Is.EqualTo(1));
            Assert.That(rig.Log.Entries.Count(entry => entry.EventName == "queue_refused"), Is.EqualTo(2));
        }

        private void AssertOutOfQueueWithSocketOpen()
        {
            Assert.That(queue.Phase, Is.EqualTo(QueuePhase.OutOfQueue));
            Assert.That(queue.SearchDeck, Is.Null);
            Assert.That(connection.Status.Phase, Is.EqualTo(ConnectionPhase.Connected));
            Assert.That(rig.Sockets.Latest.CloseRequests, Is.EqualTo(0));
        }
    }
}
