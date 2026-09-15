#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US1-8, FR-011, FR-020, FR-024: transição de continuação só na drenagem; descartada antes não recebe.</summary>
    public class StageChangeDeliveryTests
    {
        private FacadeTestRig rig = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
        }

        [Test]
        public async Task TransicaoDeContinuacaoSoSaiNaDrenagem()
        {
            List<ClientStageChange> changes = new List<ClientStageChange>();
            rig.Client.StageChanged.Subscribe(changes.Add);

            await SignInWithoutPumpAsync();
            Assert.That((changes.Count, rig.Client.State.Stage), Is.EqualTo((0, ClientStage.SignedOut)));

            rig.Pump();
            Assert.That((changes.Count, rig.Client.State.Stage), Is.EqualTo((1, ClientStage.SignedIn)));
        }

        [Test]
        public async Task AssinaturaDescartadaAntesDaDrenagemNaoRecebe()
        {
            List<ClientStageChange> dropped = new List<ClientStageChange>();
            List<ClientStageChange> kept = new List<ClientStageChange>();
            IDisposable subscription = rig.Client.StageChanged.Subscribe(dropped.Add);
            rig.Client.StageChanged.Subscribe(kept.Add);

            await SignInWithoutPumpAsync();
            subscription.Dispose();
            rig.Pump();

            Assert.That((dropped.Count, kept.Count), Is.EqualTo((0, 1)));
            Assert.DoesNotThrow(subscription.Dispose);
        }

        [Test]
        public async Task CadaTransicaoDoCaminhoPrincipalSaiUmaVezEmOrdem()
        {
            List<ClientStage> stages = new List<ClientStage>();
            rig.Client.StageChanged.Subscribe(change => stages.Add(change.Current.Stage));

            await rig.ReachInMatchAsync();
            rig.ReceiveMatchFixture(FacadeTestRig.MatchFinished);
            rig.Client.ReturnToLobby();

            Assert.That(stages, Is.EqualTo(new[] { ClientStage.SignedIn, ClientStage.Searching, ClientStage.Paired, ClientStage.InMatch, ClientStage.MatchFinished, ClientStage.SignedIn }));
        }

        private async Task SignInWithoutPumpAsync()
        {
            rig.Http.RespondNext(200, FakeAccountResponses.Login(FakeAccessJwt.FiveMinutes("login"), "refresh-1"));
            SignInOutcome outcome = await rig.Client.Account.SignInAsync("one", new Password("123456"));
            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.SignedIn));
        }
    }
}
