#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class PlayerDecksTests
    {
        private const string DeckJson = "{\"deck_id\": 4, \"name\": \"Agro\", \"card_ids\": [5, 5, 5, 7]}";

        [Test]
        public async Task SemDeckEhListaVazia()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, "{\"decks\": []}");

            AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> listed = await Decks(rig).ListAsync();

            Assert.That(listed.Value, Is.Empty);
        }

        [Test]
        public async Task ListaTrazOsDecksComIdentidadeNomeECartas()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, "{\"decks\": [{\"deck_id\": 1, \"name\": \"Deck inicial\", \"card_ids\": [1, 1, 2]}, " + DeckJson + "]}");

            IReadOnlyList<PlayerDeck> decks = (await Decks(rig).ListAsync()).Value;

            Assert.That(decks.Select(deck => deck.Deck), Is.EqualTo(new[] { new DeckId(1), new DeckId(4) }));
            Assert.That(decks[1].Name, Is.EqualTo("Agro"));
            Assert.That(decks[1].Cards, Is.EqualTo(new[] { new CardId(5), new CardId(5), new CardId(5), new CardId(7) }));
        }

        [Test]
        public async Task LerUmDeckUsaAUrlDoDeck()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, DeckJson);

            AccountCallOutcome<PlayerDeck, DeckRefusal> deck = await Decks(rig).ReadAsync(new DeckId(4));

            Assert.That(deck.Value.Deck, Is.EqualTo(new DeckId(4)));
            Assert.That(rig.Http.Requests[1].Url.AbsolutePath, Is.EqualTo("/players/decks/4/"));
        }

        [Test]
        public async Task DeckInexistenteOuDeOutroEhNaoEncontrado()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(404, "{\"detail\": \"Não encontrado.\"}");
            rig.Http.RespondNext(404, "{\"detail\": \"Não encontrado.\"}");
            rig.Http.RespondNext(404, "{\"detail\": \"Não encontrado.\"}");

            AccountCallOutcome<PlayerDeck, DeckRefusal> read = await Decks(rig).ReadAsync(new DeckId(77));
            AccountCallOutcome<PlayerDeck, DeckRefusal> changed = await Decks(rig).ChangeAsync(new DeckId(77), new DeckChange(name: "x"));
            AccountCallOutcome<DeckDeleted, DeckRefusal> deleted = await Decks(rig).DeleteAsync(new DeckId(77));

            Assert.That(read.Refusal!.Reasons.Single(), Is.SameAs(DeckNotFound.Instance));
            Assert.That(changed.Refusal!.Reasons.Single(), Is.SameAs(DeckNotFound.Instance));
            Assert.That(deleted.Refusal!.Reasons.Single(), Is.SameAs(DeckNotFound.Instance));
        }

        [Test]
        public async Task CriarEnviaNomeEListaComoVieramSemValidar()
        {
            AccountTestRig rig = await SignedInRig();
            CardId[] twelve = Enumerable.Range(1, 12).Select(value => new CardId(value)).ToArray();
            rig.Http.RespondNext(201, "{\"deck_id\": 9, \"name\": \"Curto\", \"card_ids\": [1]}");

            AccountCallOutcome<PlayerDeck, DeckRefusal> created = await Decks(rig).CreateAsync(new DeckDraft("Curto", twelve));

            IPayloadReader body = AccountTestCodec.Reader(rig.Http.Requests[1].Body!);
            Assert.That(created.Value.Deck, Is.EqualTo(new DeckId(9)));
            Assert.That(rig.Http.Requests[1].Method, Is.EqualTo("POST"));
            Assert.That(body.ReadText("name"), Is.EqualTo("Curto"));
            Assert.That(body.ReadIntegerList("card_ids").Count, Is.EqualTo(12));
        }

        [Test]
        public async Task CriarDozeCartasEhRecusadoComTamanhoErrado()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(400, "{\"deck_problems\": [{\"kind\": \"wrong_deck_size\", \"found\": 12, \"required\": 40, \"message\": \"deck has 12 cards, expected exactly 40\"}]}");

            AccountCallOutcome<PlayerDeck, DeckRefusal> created = await Decks(rig).CreateAsync(new DeckDraft("Curto", new[] { new CardId(1) }));

            WrongDeckSize size = (WrongDeckSize)((DeckListRejected)created.Refusal!.Reasons.Single()).Problems.Single();
            Assert.That((size.Found, size.Required), Is.EqualTo((12L, 40L)));
        }

        [Test]
        public async Task AlterarSoONomeEnviaSoONome()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, DeckJson.Replace("Agro", "Agro v2"));

            AccountCallOutcome<PlayerDeck, DeckRefusal> renamed = await Decks(rig).ChangeAsync(new DeckId(4), new DeckChange(name: "Agro v2"));

            Assert.That(renamed.Value.Name, Is.EqualTo("Agro v2"));
            Assert.That(rig.Http.Requests[1].Method, Is.EqualTo("PATCH"));
            Assert.That(AccountTestCodec.Reader(rig.Http.Requests[1].Body!).FieldNames, Is.EquivalentTo(new[] { "name" }));
        }

        [Test]
        public async Task ApagarCom204SemCorpoEhSucesso()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(204, "");

            AccountCallOutcome<DeckDeleted, DeckRefusal> deleted = await Decks(rig).DeleteAsync(new DeckId(4));

            Assert.That(deleted.Value, Is.SameAs(DeckDeleted.Instance));
            Assert.That(rig.Http.Requests[1].Method, Is.EqualTo("DELETE"));
        }

        [Test]
        public async Task PaginaDeProxyNaoEhReconhecida()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(502, "<html>Bad Gateway</html>");

            AccountCallOutcome<PlayerDeck, DeckRefusal> created = await Decks(rig).CreateAsync(new DeckDraft("Agro", new[] { new CardId(1) }));

            UnrecognizedDeckRefusal unrecognized = (UnrecognizedDeckRefusal)created.Refusal!.Reasons.Single();
            Assert.That(unrecognized.Status, Is.EqualTo(502));
            Assert.That(unrecognized.BodyText, Does.Contain("Bad Gateway"));
        }

        [Test]
        public async Task QuatrocentosSemChaveConhecidaNaoEhReconhecido()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(400, "{}");

            AccountCallOutcome<PlayerDeck, DeckRefusal> changed = await Decks(rig).ChangeAsync(new DeckId(4), new DeckChange(name: "x"));

            Assert.That(changed.Refusal!.Reasons.Single(), Is.InstanceOf<UnrecognizedDeckRefusal>());
        }

        [Test]
        public async Task ListaCom404NaoEhDeckNaoEncontrado()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(404, "{\"detail\": \"Não encontrado.\"}");

            AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> listed = await Decks(rig).ListAsync();

            Assert.That(listed.Refusal!.Reasons.Single(), Is.InstanceOf<UnrecognizedDeckRefusal>());
        }

        private static PlayerDecks Decks(AccountTestRig rig) => new PlayerDecks(rig.Client, rig.Codec, rig.Log, rig.Routes);

        private static async Task<AccountTestRig> SignedInRig()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            return rig;
        }
    }
}
