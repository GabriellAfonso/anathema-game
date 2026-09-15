#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US3-1 a US3-4, FR-003, FR-004: catálogo uma vez por sessão, consulta por CardId, decks pela fachada.</summary>
    public class ClientDataTests
    {
        private const string TwelveCardsRefused = "{\"deck_problems\": [{\"kind\": \"wrong_deck_size\", \"found\": 12, \"required\": 40, \"message\": \"deck has 12 cards, expected exactly 40\"}]}";

        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public async Task DuasConsultasAoCatalogoFazemUmPedidoSo()
        {
            await rig.SignInAsync();
            rig.Http.RespondNext(200, FacadeCatalog.Body());

            LoadedCatalog first = (await rig.Client.Catalog.LoadAsync()).Value;
            LoadedCatalog second = (await rig.Client.Catalog.LoadAsync()).Value;

            Assert.That(CatalogReads(), Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public async Task ConsultaPorCardIdDizUnidadeFeiticoOuNaoEncontrada()
        {
            await rig.SignInAsync();
            rig.Http.RespondNext(200, FacadeCatalog.Body());

            LoadedCatalog catalog = (await rig.Client.Catalog.LoadAsync()).Value;

            Assert.That(catalog.Find(new CardId(1)).Kind, Is.EqualTo(CardLookupKind.Unit));
            Assert.That(catalog.Find(new CardId(1001)).Kind, Is.EqualTo(CardLookupKind.Spell));
            Assert.That(catalog.Find(new CardId(424242)).Kind, Is.EqualTo(CardLookupKind.NotFound));
        }

        [Test]
        public async Task DeckDeDozeCartasEhRecusadoComTamanhoErrado()
        {
            await rig.SignInAsync();
            rig.Http.RespondNext(400, TwelveCardsRefused);
            CardId[] twelve = Enumerable.Range(1, 12).Select(card => new CardId(card)).ToArray();

            AccountCallOutcome<PlayerDeck, DeckRefusal> refused = await rig.Client.Decks.CreateAsync(new DeckDraft("Curto", twelve));

            WrongDeckSize size = refused.Refusal!.Reasons.OfType<DeckListRejected>().Single().Problems.OfType<WrongDeckSize>().Single();
            Assert.That((size.Found, size.Required), Is.EqualTo((12L, 40L)));
        }

        [Test]
        public async Task SessaoNovaPedeOCatalogoDeNovo()
        {
            await rig.SignInAsync();
            rig.Http.RespondNext(200, FacadeCatalog.Body());
            await rig.Client.Catalog.LoadAsync();
            rig.Client.Account.SignOut();
            await rig.SignInAsync();
            rig.Http.RespondNext(200, FacadeCatalog.Body());

            await rig.Client.Catalog.LoadAsync();

            Assert.That(CatalogReads(), Is.EqualTo(2));
        }

        private int CatalogReads() => rig.Http.Requests.Count(request => request.Url.AbsolutePath == "/game/cards/");
    }
}
