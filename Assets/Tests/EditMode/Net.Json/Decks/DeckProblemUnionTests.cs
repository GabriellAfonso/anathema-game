#nullable enable
using System.Linq;
using Anathema.Net.Core;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    public class DeckProblemUnionTests
    {
        private const string ThreeProblems = "{\"deck_problems\": ["
            + "{\"kind\": \"wrong_deck_size\", \"found\": 39, \"required\": 40, \"message\": \"deck has 39 cards, expected exactly 40\"}, "
            + "{\"kind\": \"too_many_copies\", \"card_id\": 12, \"count\": 4, \"limit\": 3, \"message\": \"card_id 12 appears 4 times, limit is 3\"}, "
            + "{\"kind\": \"unknown_card\", \"card_id\": 9999, \"message\": \"card_id 9999 is not in the catalog\"}]}";

        [Test]
        public void OsTresKindsDoContratoViramTiposComSeusCampos()
        {
            DeckProblem[] problems = Read(ThreeProblems);

            WrongDeckSize size = (WrongDeckSize)problems[0];
            TooManyCopies copies = (TooManyCopies)problems[1];
            UnknownCard unknown = (UnknownCard)problems[2];
            Assert.That((size.Found, size.Required), Is.EqualTo((39L, 40L)));
            Assert.That((copies.Card, copies.Count, copies.Limit), Is.EqualTo((new CardId(12), 4L, 3L)));
            Assert.That(unknown.Card, Is.EqualTo(new CardId(9999)));
            Assert.That(unknown.Message, Is.EqualTo("card_id 9999 is not in the catalog"));
        }

        [Test]
        public void KindNovoViraDesconhecidoComAMensagem()
        {
            DeckProblem problem = Read("{\"deck_problems\": [{\"kind\": \"banned_card\", \"card_id\": 3, \"message\": \"card 3 is banned\"}]}").Single();

            UnrecognizedDeckProblem unrecognized = (UnrecognizedDeckProblem)problem;
            Assert.That(unrecognized.Kind, Is.EqualTo("banned_card"));
            Assert.That(unrecognized.Message, Is.EqualTo("card 3 is banned"));
        }

        [Test]
        public void KindNovoSemMensagemFalha()
        {
            IPayloadReader item = Reader("{\"deck_problems\": [{\"kind\": \"banned_card\"}]}").ReadObjectList("deck_problems")[0];

            DecodeOutcome<DeckProblem> outcome = DeckProblemUnion.Create().DecodeObject(item);

            Assert.That(outcome.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(outcome.Failure.Path, Is.EqualTo("deck_problems[0].message"));
        }

        [Test]
        public void CardIdEmTextoEhTipoErrado()
        {
            IPayloadReader item = Reader("{\"kind\": \"unknown_card\", \"card_id\": \"9999\", \"message\": \"x\"}");

            DecodeOutcome<DeckProblem> outcome = DeckProblemUnion.Create().DecodeObject(item);

            Assert.That(outcome.Failure.Kind, Is.EqualTo(DecodeFailureKind.WrongFieldType));
        }

        [Test]
        public void ProblemaDeDeckMoraNoNucleoSemDependerDeHttp()
        {
            Assert.That(typeof(DeckProblem).Assembly.GetName().Name, Is.EqualTo("Anathema.Net.Core"));
        }

        private static DeckProblem[] Read(string json)
        {
            return Reader(json).ReadObjectList("deck_problems").Select(DeckProblemUnion.Create().ReadNested).ToArray();
        }
    }
}
