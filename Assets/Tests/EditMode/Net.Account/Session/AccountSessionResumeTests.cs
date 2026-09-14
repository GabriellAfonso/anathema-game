#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccountSessionResumeTests
    {
        [Test]
        public async Task GuardaValidaRetomaSemSenha()
        {
            AccountTestRig rig = RigWithStoredRefresh();
            rig.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("resumed")));
            int before = rig.Session.Generation;

            ResumeOutcome resumed = await rig.Session.ResumeAsync();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Resumed));
            Assert.That(resumed.User, Is.EqualTo(new UserId(7)));
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedIn));
            Assert.That(rig.Session.Self, Is.EqualTo(new UserId(7)));
            Assert.That(rig.Session.Generation, Is.EqualTo(before + 1));
            Assert.That(rig.Http.Requests[0].Url, Is.EqualTo(rig.Routes.Refresh));
            Assert.That(AccountTestCodec.Reader(rig.Http.Requests[0].Body!).ReadText("refresh"), Is.EqualTo(AccountTestRig.RefreshText));
            rig.Log.Single("account_resumed");
        }

        [Test]
        public async Task RefreshRecusadoApagaAGuardaSemAvisoDeExpiracao()
        {
            AccountTestRig rig = RigWithStoredRefresh();
            int expired = 0;
            rig.Session.SessionExpired += () => expired++;
            rig.Http.RespondNext(401, "{}");

            ResumeOutcome resumed = await rig.Session.ResumeAsync();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Refused));
            Assert.That(rig.Vault.Stored, Is.Null);
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedOut));
            Assert.That(expired, Is.EqualTo(0));
        }

        [Test]
        public async Task SemRedeMantemAGuardaParaOutraTentativa()
        {
            AccountTestRig rig = RigWithStoredRefresh();
            rig.Http.FailNext(TransportFailureKind.CannotConnect, "Cannot connect to destination host");

            ResumeOutcome resumed = await rig.Session.ResumeAsync();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Unavailable));
            Assert.That(resumed.Renewal!.Reason, Is.EqualTo(RenewalUnavailableReason.Transport));
            Assert.That(rig.Vault.Stored, Is.EqualTo(AccountTestRig.RefreshText));
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedOut));
        }

        [Test]
        public async Task ErroDoServidorMantemAGuarda()
        {
            AccountTestRig rig = RigWithStoredRefresh();
            rig.Http.RespondNext(503, "<html>");

            ResumeOutcome resumed = await rig.Session.ResumeAsync();

            Assert.That(resumed.Renewal!.Reason, Is.EqualTo(RenewalUnavailableReason.ServerStatus));
            Assert.That(rig.Vault.Stored, Is.EqualTo(AccountTestRig.RefreshText));
        }

        [Test]
        public async Task NadaGuardadoNaoPedeNada()
        {
            AccountTestRig rig = new AccountTestRig();

            ResumeOutcome resumed = await rig.Session.ResumeAsync();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.NothingStored));
            Assert.That(rig.Http.Requests, Is.Empty);
        }

        [Test]
        public async Task GuardaIlegivelViraNadaGuardadoEEhApagada()
        {
            AccountTestRig rig = RigWithStoredRefresh();
            rig.Vault.MakeUnreadable("key_invalidated");

            ResumeOutcome resumed = await rig.Session.ResumeAsync();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.NothingStored));
            Assert.That(rig.Vault.DeleteCount, Is.EqualTo(1));
            Assert.That(rig.Http.Requests, Is.Empty);
            Assert.That(rig.Log.Single("vault_unreadable").Fields[0].Value, Is.EqualTo("key_invalidated"));
        }

        [Test]
        public async Task JaAutenticadoNaoPedeNada()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();

            ResumeOutcome resumed = await rig.Session.ResumeAsync();

            Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Resumed));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DepoisDeRetomarOTokenValeSemNovoPedido()
        {
            AccountTestRig rig = RigWithStoredRefresh();
            rig.Http.RespondNext(200, FakeAccountResponses.Refresh(FakeAccessJwt.FiveMinutes("resumed")));
            await rig.Session.ResumeAsync();

            AccessTokenOutcome token = await rig.Tokens.GetValidAsync();

            Assert.That(token.Token!.RevealForRequest(), Is.EqualTo(FakeAccessJwt.FiveMinutes("resumed")));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(1));
        }

        private static AccountTestRig RigWithStoredRefresh()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Vault.Preload(AccountTestRig.RefreshText);
            return rig;
        }
    }
}
