#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>US1-1, US1-2, FR-010: deslogado na abertura, logado por login ou retomada.</summary>
    public class SignInStageTests
    {
        private FacadeTestRig rig = null!;
        private List<ClientStageChange> changes = null!;

        [SetUp]
        public void CreateRig()
        {
            rig = new FacadeTestRig();
            changes = new List<ClientStageChange>();
            rig.Client.StageChanged.Subscribe(changes.Add);
        }

        [Test]
        public async Task RetomarSemGuardaContinuaDeslogado()
        {
            ResumeOutcome resumed = await rig.Client.Account.ResumeAsync();
            rig.Pump();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.NothingStored));
            Assert.That((rig.Client.State.Stage, changes.Count, rig.Http.Requests.Count), Is.EqualTo((ClientStage.SignedOut, 0, 0)));
        }

        [Test]
        public async Task EntrarLevaALogadoComOUsuario()
        {
            await rig.SignInAsync();

            Assert.That((rig.Client.State.Stage, rig.Client.State.Self), Is.EqualTo((ClientStage.SignedIn, (UserId?)new UserId(7))));
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That((changes[0].Previous.Stage, changes[0].Current.Stage), Is.EqualTo((ClientStage.SignedOut, ClientStage.SignedIn)));
        }

        [Test]
        public async Task RetomarComGuardaLevaALogadoComOUsuario()
        {
            rig.Vault.Preload("refresh-1");
            rig.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("resumed")));

            ResumeOutcome resumed = await rig.Client.Account.ResumeAsync();
            rig.Pump();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Resumed));
            Assert.That((rig.Client.State.Stage, rig.Client.State.Self), Is.EqualTo((ClientStage.SignedIn, (UserId?)new UserId(7))));
        }
    }
}
