#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class TokenRenewalTests
    {
        [Test]
        public async Task RenovacaoAceitaTrocaOAcessoEMantemORefresh()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));

            RenewalOutcome outcome = await rig.Session.Renewal.RenewAsync();

            Assert.That(outcome.Kind, Is.EqualTo(RenewalOutcomeKind.Renewed));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.EqualTo(FakeAccessJwt.FiveMinutes("renewed")));
            Assert.That(rig.Session.CurrentTokens!.Refresh.RevealForRequest(), Is.EqualTo(AccountTestRig.RefreshText));
            Assert.That(AccountTestCodec.Reader(rig.Http.Requests[1].Body!).ReadText("refresh"), Is.EqualTo(AccountTestRig.RefreshText));
            Assert.That(rig.Http.Requests[1].Url, Is.EqualTo(rig.Routes.Refresh));
            rig.Log.Single("access_token_renewed");
        }

        [Test]
        public async Task RefreshRecusadoExpiraASessaoUmaVez()
        {
            AccountTestRig rig = await SignedInRig();
            int expired = 0;
            rig.Session.SessionExpired += () => expired++;
            rig.Http.RespondNext(401, "{\"detail\": \"Token is invalid or expired\"}");

            RenewalOutcome outcome = await rig.Session.Renewal.RenewAsync();

            Assert.That(outcome.Kind, Is.EqualTo(RenewalOutcomeKind.SessionExpired));
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.Expired));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.Null);
            Assert.That(rig.Session.Self, Is.Null);
            Assert.That(rig.Vault.Stored, Is.Null);
            Assert.That(expired, Is.EqualTo(1));
            rig.Log.Single("session_expired");
        }

        [TestCase(400)]
        [TestCase(503)]
        public async Task StatusDiferenteDe401NaoExpira(int status)
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(status, "{}");

            RenewalOutcome outcome = await rig.Session.Renewal.RenewAsync();

            Assert.That(outcome.Kind, Is.EqualTo(RenewalOutcomeKind.Unavailable));
            Assert.That(outcome.Reason, Is.EqualTo(RenewalUnavailableReason.ServerStatus));
            AssertStillSignedIn(rig);
        }

        [Test]
        public async Task FalhaDeTransporteNaoExpira()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.FailNext(TransportFailureKind.Timeout, "Request timeout");

            RenewalOutcome outcome = await rig.Session.Renewal.RenewAsync();

            Assert.That(outcome.Reason, Is.EqualTo(RenewalUnavailableReason.Transport));
            Assert.That(outcome.Transport!.Kind, Is.EqualTo(TransportFailureKind.Timeout));
            AssertStillSignedIn(rig);
        }

        [Test]
        public async Task RespostaSemAccessNaoExpira()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, "{}");

            RenewalOutcome outcome = await rig.Session.Renewal.RenewAsync();

            Assert.That(outcome.Reason, Is.EqualTo(RenewalUnavailableReason.OutOfContract));
            AssertStillSignedIn(rig);
        }

        [Test]
        public async Task RenovacaoQueTerminaDepoisDeSairEhDescartada()
        {
            AccountTestRig rig = await SignedInRig();
            HeldHttpResponse held = rig.Http.HoldNext();
            Task<RenewalOutcome> renewing = rig.Session.Renewal.RenewAsync();

            rig.Session.SignOut();
            held.Release(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));

            Assert.That((await renewing).Kind, Is.EqualTo(RenewalOutcomeKind.Renewed));
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedOut));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.Null);
        }

        [Test]
        public async Task ChamadasDuranteARenovacaoRecebemAMesmaTarefa()
        {
            AccountTestRig rig = await SignedInRig();
            HeldHttpResponse held = rig.Http.HoldNext();

            Task<RenewalOutcome> first = rig.Session.Renewal.RenewAsync();
            Task<RenewalOutcome> second = rig.Session.Renewal.RenewAsync();

            Assert.That(second, Is.SameAs(first));
            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(1));
            held.Release(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));
            Assert.That(await second, Is.SameAs(await first));
        }

        [Test]
        public async Task RenovacaoConcluidaNaoPrendeAProxima()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("first")));
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("second")));

            await rig.Session.Renewal.RenewAsync();
            await rig.Session.Renewal.RenewAsync();

            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(2));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.EqualTo(FakeAccessJwt.FiveMinutes("second")));
        }

        [Test]
        public async Task SemSessaoNaoPedeNada()
        {
            AccountTestRig rig = new AccountTestRig();

            RenewalOutcome outcome = await rig.Session.Renewal.RenewAsync();

            Assert.That(outcome.Kind, Is.EqualTo(RenewalOutcomeKind.NoSession));
            Assert.That(rig.Http.Requests, Is.Empty);
        }

        private static async Task<AccountTestRig> SignedInRig()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            return rig;
        }

        private static void AssertStillSignedIn(AccountTestRig rig)
        {
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedIn));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.EqualTo(FakeAccessJwt.FiveMinutes("login")));
            Assert.That(rig.Vault.Stored, Is.EqualTo(AccountTestRig.RefreshText));
            Assert.That(rig.Vault.DeleteCount, Is.EqualTo(0));
        }
    }
}
