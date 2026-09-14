#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AuthenticatedHttpClientTests
    {
        [Test]
        public async Task GetLevaBearerEAcceptSemContentType()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(200, "{}");

            await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));

            HttpRequestSpec sent = rig.Http.Requests[1];
            Assert.That(sent.Headers["Authorization"], Is.EqualTo("Bearer " + rig.Session.CurrentAccessTokenText));
            Assert.That(sent.Headers["Accept"], Is.EqualTo("application/json"));
            Assert.That(sent.Headers.ContainsKey("Content-Type"), Is.False);
        }

        [Test]
        public async Task ComCorpoLevaContentType()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(201, "{}");

            await rig.Client.SendAsync(new AuthenticatedRequest("POST", rig.Routes.Decks, "{\"name\": \"Agro\"}"));

            Assert.That(rig.Http.Requests[1].Headers["Content-Type"], Is.EqualTo("application/json"));
            Assert.That(rig.Http.Requests[1].Body, Is.EqualTo("{\"name\": \"Agro\"}"));
        }

        [Test]
        public async Task Recebe401RenovaUmaVezERepeteUmaVez()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(401, "{}");
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));
            rig.Http.RespondNext(200, "{\"ok\": true}");

            AuthenticatedCallResult result = await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));

            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(4));
            Assert.That(rig.Http.Requests[2].Url, Is.EqualTo(rig.Routes.Refresh));
            Assert.That(rig.Http.Requests[3].Headers["Authorization"], Is.EqualTo("Bearer " + FakeAccessJwt.FiveMinutes("renewed")));
        }

        [Test]
        public async Task RepeticaoCom401DevolveARecusaSemNovaRenovacao()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(401, "{}");
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));
            rig.Http.RespondNext(401, "{\"detail\": \"no\"}");

            AuthenticatedCallResult result = await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));

            Assert.That(result.Failure, Is.Null);
            Assert.That(result.Status, Is.EqualTo(401));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task DuasChamadasCom401CompartilhamUmaRenovacao()
        {
            AccountTestRig rig = await SignedInRig();
            HeldHttpResponse first = rig.Http.HoldNext();
            HeldHttpResponse second = rig.Http.HoldNext();
            HeldHttpResponse refresh = rig.Http.HoldNext();
            rig.Http.RespondNext(200, "{}");
            rig.Http.RespondNext(200, "{}");
            Task<AuthenticatedCallResult> firstCall = rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));
            Task<AuthenticatedCallResult> secondCall = rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Decks));

            first.Release(401, "{}");
            second.Release(401, "{}");
            refresh.Release(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));

            Assert.That((await firstCall).Status, Is.EqualTo(200));
            Assert.That((await secondCall).Status, Is.EqualTo(200));
            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(1));
            Assert.That(rig.Http.Requests.Count, Is.EqualTo(6));
        }

        [Test]
        public async Task TokenJaTrocadoPorOutraChamadaRepeteSemRenovar()
        {
            AccountTestRig rig = await SignedInRig();
            HeldHttpResponse rejected = rig.Http.HoldNext();
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));
            rig.Http.RespondNext(200, "{}");
            Task<AuthenticatedCallResult> call = rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));
            await rig.Tokens.RenewNowAsync();

            rejected.Release(401, "{}");

            Assert.That((await call).Status, Is.EqualTo(200));
            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(1));
            Assert.That(rig.Http.Requests.Last().Headers["Authorization"], Is.EqualTo("Bearer " + FakeAccessJwt.FiveMinutes("renewed")));
        }

        [Test]
        public async Task RefreshRecusadoDuranteO401ViraSessaoExpirada()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(401, "{}");
            rig.Http.RespondNext(401, "{}");

            AuthenticatedCallResult result = await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));

            Assert.That(result.Failure!.Kind, Is.EqualTo(AccountCallFailureKind.SessionUnavailable));
            Assert.That(result.Failure.Session, Is.EqualTo(SessionUnavailableKind.Expired));
        }

        [Test]
        public async Task FalhaDeTransporteNaoRenova()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.FailNext(TransportFailureKind.CannotConnect, "Cannot connect to destination host");

            AuthenticatedCallResult result = await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));

            Assert.That(result.Failure!.Kind, Is.EqualTo(AccountCallFailureKind.TransportFailed));
            Assert.That(rig.RequestsTo(rig.Routes.Refresh), Is.EqualTo(0));
        }

        [Test]
        public async Task SemLoginNaoPedeNada()
        {
            AccountTestRig rig = new AccountTestRig();

            AuthenticatedCallResult result = await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));

            Assert.That(result.Failure!.Session, Is.EqualTo(SessionUnavailableKind.NoSession));
            Assert.That(rig.Http.Requests, Is.Empty);
        }

        [Test]
        public async Task NenhumRegistroTemOToken()
        {
            AccountTestRig rig = await SignedInRig();
            rig.Http.RespondNext(401, "{}");
            rig.Http.RespondNext(200, AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")));
            rig.Http.RespondNext(200, "{}");

            await rig.Client.SendAsync(AuthenticatedRequest.Get(rig.Routes.Cards));

            string[] logged = rig.Log.Entries.SelectMany(entry => entry.Fields.Select(field => field.Value)).ToArray();
            Assert.That(logged, Has.None.Contains(FakeAccessJwt.FiveMinutes("login")).And.None.Contains(FakeAccessJwt.FiveMinutes("renewed")));
        }

        private static async Task<AccountTestRig> SignedInRig()
        {
            AccountTestRig rig = new AccountTestRig();
            await rig.SignInAsync();
            return rig;
        }
    }
}
