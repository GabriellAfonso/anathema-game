#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>FR-001, FR-003, FR-004: a fachada composta por construtor, deslogada na abertura, com os serviços da mesma conta.</summary>
    public class AnathemaClientCompositionTests
    {
        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public void ComecaDeslogadaNaAbertura()
        {
            Assert.That((rig.Client.State.Stage, rig.Client.State.SignedOutReason), Is.EqualTo((ClientStage.SignedOut, (SignedOutReason?)SignedOutReason.Startup)));
        }

        [Test]
        public void ServicosDeDadosSaoDaMesmaContaComposta()
        {
            Assert.That(rig.Client.Catalog, Is.SameAs(rig.Client.AccountParts.Catalog));
            Assert.That(rig.Client.Decks, Is.SameAs(rig.Client.AccountParts.Decks));
            Assert.That(rig.Client.History, Is.SameAs(rig.Client.AccountParts.History));
            Assert.That(rig.Client.Log, Is.SameAs(rig.Log));
        }

        [Test]
        public async Task DescartarParaDeRenovarNaVoltaAoPrimeiroPlano()
        {
            rig.Http.RespondNext(200, FakeAccountResponses.Login(FakeAccessJwt.FiveMinutes("login"), "refresh-1"));
            await rig.Client.AccountParts.Session.SignInAsync("one", new Password("123456"));
            rig.Lifecycle.SimulateBackground();
            rig.Clock.Advance(TimeSpan.FromMinutes(7));

            rig.Client.Dispose();
            rig.Lifecycle.SimulateForeground();

            Assert.That(rig.Http.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void DescartarDuasVezesNaoFalhaEInvalidaUmaVez()
        {
            rig.Client.Dispose();

            Assert.DoesNotThrow(rig.Client.Dispose);
            Assert.That(rig.Client.Stages.Generation, Is.EqualTo(1));
        }
    }
}
