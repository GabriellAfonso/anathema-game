#nullable enable
using System.Linq;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class DeckRefusalReaderTests
    {
        [Test]
        public void NomeInvalidoTrazAsMensagens()
        {
            DeckRefusal refusal = Read("{\"name\": [\"name is '   ': expected a non-empty name of up to 50 characters\"]}");

            InvalidDeckName invalid = (InvalidDeckName)refusal.Reasons.Single();
            Assert.That(invalid.Messages.Single(), Does.StartWith("name is"));
        }

        [Test]
        public void ProblemasDaListaChegamJuntosNaOrdem()
        {
            DeckRefusal refusal = Read("{\"deck_problems\": ["
                + "{\"kind\": \"wrong_deck_size\", \"found\": 39, \"required\": 40, \"message\": \"a\"}, "
                + "{\"kind\": \"too_many_copies\", \"card_id\": 12, \"count\": 4, \"limit\": 3, \"message\": \"b\"}, "
                + "{\"kind\": \"unknown_card\", \"card_id\": 9999, \"message\": \"c\"}]}");

            DeckListRejected rejected = (DeckListRejected)refusal.Reasons.Single();
            Assert.That(rejected.Problems.Select(problem => problem.GetType()), Is.EqualTo(new[] { typeof(WrongDeckSize), typeof(TooManyCopies), typeof(UnknownCard) }));
        }

        [Test]
        public void TetoDeDecksTrazAsMensagens()
        {
            DeckRefusal refusal = Read("{\"deck_limit\": [\"player already has 20 decks: the limit is 20\"]}");

            Assert.That(((DeckLimitReached)refusal.Reasons.Single()).Messages.Single(), Does.Contain("20 decks"));
        }

        [Test]
        public void CampoFaltandoNaCriacaoTrazODetalhe()
        {
            DeckRefusal refusal = Read("{\"detail\": \"creating a deck needs both 'name' and 'card_ids'\"}");

            Assert.That(((MissingDeckField)refusal.Reasons.Single()).Detail, Does.Contain("needs both"));
        }

        [Test]
        public void NomeEProblemasNoMesmoCorpoVemJuntosNaOrdemDaTabela()
        {
            DeckRefusal refusal = Read("{\"deck_problems\": [{\"kind\": \"unknown_card\", \"card_id\": 9999, \"message\": \"c\"}], \"name\": [\"bad\"]}");

            Assert.That(refusal.Reasons.Select(reason => reason.GetType()), Is.EqualTo(new[] { typeof(InvalidDeckName), typeof(DeckListRejected) }));
        }

        [Test]
        public void CorpoSemChaveConhecidaLanca()
        {
            Assert.Throws<PayloadShapeException>(() => new DeckRefusalReader().ReadBadRequest(AccountTestCodec.Reader("{\"other\": 1}")));
        }

        private static DeckRefusal Read(string json) => new DeckRefusalReader().ReadBadRequest(AccountTestCodec.Reader(json));
    }
}
