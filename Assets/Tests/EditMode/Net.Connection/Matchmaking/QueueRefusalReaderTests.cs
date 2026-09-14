#nullable enable
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Tabela "QueueRefusal" de contracts/matchmaking-queue.md.</summary>
    public class QueueRefusalReaderTests
    {
        private FakeClientLog log = null!;
        private QueueRefusalReader reader = null!;

        [SetUp]
        public void CreateReader()
        {
            log = new FakeClientLog();
            reader = new QueueRefusalReader(log);
        }

        [TestCase("deck_not_specified", QueueRefusalKind.DeckNotSpecified)]
        [TestCase("malformed_message", QueueRefusalKind.MalformedMessage)]
        [TestCase("unknown_message_type", QueueRefusalKind.UnknownMessageType)]
        [TestCase("brand_new_code", QueueRefusalKind.Unrecognized)]
        public void CodigoSemCamposExtras(string code, QueueRefusalKind kind)
        {
            QueueRefusal refusal = Read(TestFrames.Refused(code));

            Assert.That(refusal.Kind, Is.EqualTo(kind));
            Assert.That(refusal.Code, Is.EqualTo(code));
            Assert.That(refusal.Error, Is.EqualTo("refused"));
            Assert.That(refusal.Deck, Is.Null);
            Assert.That(refusal.Problems, Is.Empty);
        }

        [Test]
        public void DeckNotFoundEcoaODeck()
        {
            QueueRefusal refusal = Read(TestFrames.DeckNotFound);

            Assert.That(refusal.Kind, Is.EqualTo(QueueRefusalKind.DeckNotFound));
            Assert.That(refusal.Deck, Is.EqualTo(new DeckId(4)));
        }

        [Test]
        public void InvalidDeckTrazOsProblemasNaOrdemDoServidor()
        {
            QueueRefusal refusal = Read(TestFrames.InvalidDeck);

            Assert.That(refusal.Kind, Is.EqualTo(QueueRefusalKind.InvalidDeck));
            Assert.That(refusal.Deck, Is.EqualTo(new DeckId(4)));
            WrongDeckSize size = (WrongDeckSize)refusal.Problems[0];
            TooManyCopies copies = (TooManyCopies)refusal.Problems[1];
            Assert.That((size.Found, size.Required), Is.EqualTo((39L, 40L)));
            Assert.That((copies.Card, copies.Count, copies.Limit), Is.EqualTo((new CardId(12), 4L, 3L)));
        }

        [TestCase("deck_not_found", "deck_id")]
        [TestCase("invalid_deck", "deck_problems")]
        public void CodigoConhecidoComFormaQuebradaViraNaoReconhecidoERegistra(string code, string field)
        {
            QueueRefusal refusal = Read(TestFrames.Refused(code));

            Assert.That(refusal.Kind, Is.EqualTo(QueueRefusalKind.Unrecognized));
            Assert.That(refusal.Code, Is.EqualTo(code));
            ClientLogEntry entry = log.Single("queue_refusal_out_of_contract");
            Assert.That(entry.Fields.Single(logField => logField.Name == "path").Value, Does.Contain(field));
        }

        [Test]
        public void TextoDoErroNaoMudaOTipo()
        {
            string otherText = TestFrames.DeckNotFound.Replace("is not a deck of user 9", "was deleted");

            Assert.That(Read(otherText).Kind, Is.EqualTo(Read(TestFrames.DeckNotFound).Kind));
        }

        private QueueRefusal Read(string json)
        {
            return reader.Read((MessageRefusedFrame)ConnectionTestCodec.Codec().Decode(json).Value);
        }
    }
}
