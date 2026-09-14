#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class CardCatalogTests
    {
        private static readonly string OneCard = CatalogJson.Body(CatalogJson.Unit(1));

        [Test]
        public async Task DuasBuscasConcorrentesFazemUmPedido()
        {
            AccountTestRig rig = await SignedInRig();
            CardCatalog catalog = Catalog(rig);
            HeldHttpResponse held = rig.Http.HoldNext();

            Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>> first = catalog.LoadAsync();
            Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>> second = catalog.LoadAsync();
            held.Release(200, OneCard);

            Assert.That(rig.RequestsTo(rig.Routes.Cards), Is.EqualTo(1));
            Assert.That((await first).Value, Is.SameAs((await second).Value));
        }

        [Test]
        public async Task CatalogoCarregadoNaoPedeDeNovo()
        {
            AccountTestRig rig = await SignedInRig();
            CardCatalog catalog = Catalog(rig);
            rig.Http.RespondNext(200, OneCard);

            await catalog.LoadAsync();
            AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> again = await catalog.LoadAsync();

            Assert.That(again.Value.Find(new CardId(1)).Kind, Is.EqualTo(CardLookupKind.Unit));
            Assert.That(rig.RequestsTo(rig.Routes.Cards), Is.EqualTo(1));
        }

        [Test]
        public async Task FalhaNaoFicaGuardada()
        {
            AccountTestRig rig = await SignedInRig();
            CardCatalog catalog = Catalog(rig);
            rig.Http.FailNext(TransportFailureKind.Timeout, "Request timeout");
            rig.Http.RespondNext(200, OneCard);

            AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> failed = await catalog.LoadAsync();
            AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> loaded = await catalog.LoadAsync();

            Assert.That(failed.Failure!.Kind, Is.EqualTo(AccountCallFailureKind.TransportFailed));
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(rig.RequestsTo(rig.Routes.Cards), Is.EqualTo(2));
        }

        [Test]
        public async Task SairEEntrarDeNovoBuscaOutraVez()
        {
            AccountTestRig rig = await SignedInRig();
            CardCatalog catalog = Catalog(rig);
            rig.Http.RespondNext(200, OneCard);
            await catalog.LoadAsync();

            rig.Session.SignOut();
            await rig.SignInAsync("second-login");
            rig.Http.RespondNext(200, OneCard);
            await catalog.LoadAsync();

            Assert.That(rig.RequestsTo(rig.Routes.Cards), Is.EqualTo(2));
        }

        [Test]
        public async Task ErroDoServidorEhRecusaNaoReconhecida()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(500, "<html>");

            AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> outcome = await Catalog(rig).LoadAsync();

            Assert.That(outcome.Refusal!.Status, Is.EqualTo(500));
        }

        [Test]
        public async Task RespostaSemCartasEhForaDoContrato()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, "{}");

            AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> outcome = await Catalog(rig).LoadAsync();

            Assert.That(outcome.Failure!.Kind, Is.EqualTo(AccountCallFailureKind.OutOfContract));
            Assert.That(outcome.Failure.Decode!.Path, Is.EqualTo("cards"));
        }

        private static CardCatalog Catalog(AccountTestRig rig) => new CardCatalog(rig.Client, rig.Session, rig.Codec, rig.Log, rig.Routes);

        private static async Task<AccountTestRig> SignedInRig()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            return rig;
        }
    }
}
