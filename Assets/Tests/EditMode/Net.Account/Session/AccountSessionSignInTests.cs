#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccountSessionSignInTests
    {
        [Test]
        public async Task LoginAceitoAutenticaGuardaORefreshEDaODono()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(200, AccountTestRig.LoginBody(FakeAccessJwt.FiveMinutes("login", "7")));
            int before = rig.Session.Generation;

            SignInOutcome outcome = await SignIn(rig);

            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.SignedIn));
            Assert.That(outcome.User, Is.EqualTo(new UserId(7)));
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedIn));
            Assert.That(rig.Session.Self, Is.EqualTo(new UserId(7)));
            Assert.That(rig.Vault.Stored, Is.EqualTo(AccountTestRig.RefreshText));
            Assert.That(rig.Session.Generation, Is.EqualTo(before + 1));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.EqualTo(FakeAccessJwt.FiveMinutes("login", "7")));
            rig.Log.Single("account_signed_in");
        }

        [Test]
        public async Task LoginVaiParaARotaDeLoginComUsuarioESenha()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(200, AccountTestRig.LoginBody(FakeAccessJwt.FiveMinutes()));

            await SignIn(rig);

            HttpRequestSpec sent = rig.Http.Requests[0];
            IPayloadReader body = AccountTestCodec.Reader(sent.Body!);
            Assert.That(sent.Method, Is.EqualTo("POST"));
            Assert.That(sent.Url, Is.EqualTo(rig.Routes.Login));
            Assert.That(body.ReadText("username"), Is.EqualTo(AccountTestRig.Username));
            Assert.That(body.ReadText("password"), Is.EqualTo(AccountTestRig.PasswordText));
        }

        [Test]
        public void SemLoginNaoHaTokenNemDono()
        {
            AccountTestRig rig = new AccountTestRig();

            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedOut));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.Null);
            Assert.That(rig.Session.Self, Is.Null);
        }

        [Test]
        public async Task CredencialErradaEhRecusaSemGuardarNada()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(401, "{\"detail\": \"Invalid credentials\"}");

            SignInOutcome outcome = await SignIn(rig);

            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.CredentialsRefused));
            AssertNothingKept(rig);
        }

        [Test]
        public async Task OutroStatusEhRecusaDoServidor()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(500, "<html>");

            SignInOutcome outcome = await SignIn(rig);

            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.ServerRefused));
            Assert.That(outcome.Status, Is.EqualTo(500));
            AssertNothingKept(rig);
        }

        [Test]
        public async Task FalhaDeTransporteEhDistinta()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.FailNext(TransportFailureKind.CannotConnect, "Cannot connect to destination host");

            SignInOutcome outcome = await SignIn(rig);

            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.TransportFailed));
            Assert.That(outcome.Transport!.Kind, Is.EqualTo(TransportFailureKind.CannotConnect));
            AssertNothingKept(rig);
        }

        [Test]
        public async Task TokenIlegivelEhForaDoContrato()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Http.RespondNext(200, AccountTestRig.LoginBody("x.y"));

            SignInOutcome outcome = await SignIn(rig);

            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.OutOfContract));
            AssertNothingKept(rig);
        }

        [Test]
        public async Task SegundoLoginDuranteOPrimeiroEhRecusadoSemPedido()
        {
            AccountTestRig rig = new AccountTestRig();
            HeldHttpResponse held = rig.Http.HoldNext();
            Task<SignInOutcome> first = SignIn(rig);

            SignInOutcome second = await SignIn(rig);

            Assert.That(second.Kind, Is.EqualTo(SignInOutcomeKind.AlreadyInProgress));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(1));
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SigningIn));
            held.Release(200, AccountTestRig.LoginBody(FakeAccessJwt.FiveMinutes()));
            Assert.That((await first).Kind, Is.EqualTo(SignInOutcomeKind.SignedIn));
        }

        [Test]
        public async Task FalhaAoGravarNaGuardaNaoImpedeOLogin()
        {
            AccountTestRig rig = new AccountTestRig();
            rig.Vault.FailNextSave("disk_full");
            rig.Http.RespondNext(200, AccountTestRig.LoginBody(FakeAccessJwt.FiveMinutes()));

            SignInOutcome outcome = await SignIn(rig);

            Assert.That(outcome.Kind, Is.EqualTo(SignInOutcomeKind.SignedIn));
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedIn));
            Assert.That(rig.Log.Single("vault_save_failed").Fields[0].Value, Is.EqualTo("disk_full"));
        }

        [Test]
        public async Task SairApagaTudoSemAvisoDeExpiracaoNemPedido()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            int expired = 0;
            rig.Session.SessionExpired += () => expired++;
            int requests = rig.Http.Requests.Count;

            rig.Session.SignOut();

            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedOut));
            Assert.That(rig.Session.Self, Is.Null);
            Assert.That(rig.Session.CurrentAccessTokenText, Is.Null);
            Assert.That(rig.Vault.Stored, Is.Null);
            Assert.That(rig.Vault.DeleteCount, Is.EqualTo(1));
            Assert.That(expired, Is.EqualTo(0));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(requests));
        }

        private static Task<SignInOutcome> SignIn(AccountTestRig rig)
        {
            return rig.Session.SignInAsync(AccountTestRig.Username, new Password(AccountTestRig.PasswordText));
        }

        private static void AssertNothingKept(AccountTestRig rig)
        {
            Assert.That(rig.Session.State, Is.EqualTo(AccountSessionState.SignedOut));
            Assert.That(rig.Session.CurrentAccessTokenText, Is.Null);
            Assert.That(rig.Vault.Stored, Is.Null);
            Assert.That(rig.Vault.SaveCount, Is.EqualTo(0));
        }
    }
}
